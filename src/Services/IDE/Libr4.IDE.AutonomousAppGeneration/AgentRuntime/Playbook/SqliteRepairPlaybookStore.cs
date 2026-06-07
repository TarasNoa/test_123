using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public sealed class SqliteRepairPlaybookStore : IRepairPlaybookStore
{
    private readonly string _connectionString;
    private readonly RepairPlaybookOptions _options;
    private readonly ILogger<SqliteRepairPlaybookStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SqliteRepairPlaybookStore(
        IOptions<RepairPlaybookOptions> options,
        ILogger<SqliteRepairPlaybookStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        var directory = Path.GetDirectoryName(_options.DbPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _options.DbPath }.ConnectionString;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS repair_playbook (
                  error_signature TEXT PRIMARY KEY,
                  stack_pattern TEXT NOT NULL,
                  fix_pattern TEXT NOT NULL,
                  success_count INTEGER NOT NULL DEFAULT 0,
                  fail_count INTEGER NOT NULL DEFAULT 0,
                  score REAL NOT NULL DEFAULT 0,
                  last_used_at TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_repair_playbook_score ON repair_playbook(score DESC, last_used_at DESC);
                """;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string?> TryGetHintAsync(string errorSignature, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(errorSignature))
            return null;

        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT fix_pattern, success_count, fail_count, score
                FROM repair_playbook
                WHERE error_signature = $sig
                """;
            cmd.Parameters.AddWithValue("$sig", errorSignature);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return null;

            var fix = reader.GetString(0);
            var successes = reader.GetInt32(1);
            var fails = reader.GetInt32(2);
            var score = reader.GetDouble(3);
            var attempts = successes + fails;
            if (attempts < _options.MinAttemptsBeforeHint || score < _options.MinScoreForHint)
                return null;

            await using var touch = conn.CreateCommand();
            touch.CommandText = "UPDATE repair_playbook SET last_used_at = $ts WHERE error_signature = $sig";
            touch.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
            touch.Parameters.AddWithValue("$sig", errorSignature);
            await touch.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            return fix;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordOutcomeAsync(
        string errorSignature,
        string fixPattern,
        bool succeeded,
        string? stackPattern = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(errorSignature) || string.IsNullOrWhiteSpace(fixPattern))
            return;

        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow.ToString("O");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);

            var existing = await ReadEntryAsync(conn, errorSignature, ct).ConfigureAwait(false);
            var stack = stackPattern ?? existing?.StackPattern ?? "unknown";
            var fix = fixPattern;
            var successes = (existing?.SuccessCount ?? 0) + (succeeded ? 1 : 0);
            var fails = (existing?.FailCount ?? 0) + (succeeded ? 0 : 1);
            var score = ComputeScore(existing?.Score ?? 0, succeeded);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO repair_playbook(error_signature, stack_pattern, fix_pattern, success_count, fail_count, score, last_used_at)
                VALUES ($sig, $stack, $fix, $succ, $fail, $score, $ts)
                ON CONFLICT(error_signature) DO UPDATE SET
                  stack_pattern = excluded.stack_pattern,
                  fix_pattern = excluded.fix_pattern,
                  success_count = excluded.success_count,
                  fail_count = excluded.fail_count,
                  score = excluded.score,
                  last_used_at = excluded.last_used_at
                """;
            cmd.Parameters.AddWithValue("$sig", errorSignature);
            cmd.Parameters.AddWithValue("$stack", stack);
            cmd.Parameters.AddWithValue("$fix", fix);
            cmd.Parameters.AddWithValue("$succ", successes);
            cmd.Parameters.AddWithValue("$fail", fails);
            cmd.Parameters.AddWithValue("$score", score);
            cmd.Parameters.AddWithValue("$ts", now);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            _logger.LogDebug(
                "Repair playbook {Outcome} sig={Signature} score={Score:F2} ({Success}/{Fail})",
                succeeded ? "success" : "fail",
                errorSignature[..Math.Min(12, errorSignature.Length)],
                score,
                successes,
                fails);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RepairPlaybookEntry?> GetBySignatureAsync(string errorSignature, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(errorSignature))
            return null;

        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            return await ReadEntryAsync(conn, errorSignature, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<RepairPlaybookEntry>> ListTopAsync(int limit = 20, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var rows = new List<RepairPlaybookEntry>();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT error_signature, stack_pattern, fix_pattern, success_count, fail_count, score, last_used_at
                FROM repair_playbook
                ORDER BY score DESC, last_used_at DESC
                LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$limit", Math.Max(1, limit));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                rows.Add(new RepairPlaybookEntry(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.GetDouble(5),
                    DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)));
            }
        }
        finally
        {
            _gate.Release();
        }

        return rows;
    }

    private double ComputeScore(double previousScore, bool succeeded)
    {
        if (succeeded)
            return Math.Min(1.0, previousScore <= 0 ? 0.6 : previousScore + 0.15);

        var decayed = previousScore <= 0 ? 0.2 : previousScore * _options.FailScoreDecay;
        return Math.Max(0.0, decayed);
    }

    private static async Task<RepairPlaybookEntry?> ReadEntryAsync(
        SqliteConnection conn,
        string signature,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT error_signature, stack_pattern, fix_pattern, success_count, fail_count, score, last_used_at
            FROM repair_playbook WHERE error_signature = $sig
            """;
        cmd.Parameters.AddWithValue("$sig", signature);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new RepairPlaybookEntry(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetDouble(5),
            DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    private SqliteConnection Open() => new(_connectionString);
}
