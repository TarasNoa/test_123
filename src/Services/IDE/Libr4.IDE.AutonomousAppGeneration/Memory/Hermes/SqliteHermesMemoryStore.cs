using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class SqliteHermesMemoryStore : IHermesMemoryStore, IMemoryStore
{
    private readonly string _connectionString;
    private readonly HermesMemoryOptions _options;
    private readonly ILogger<SqliteHermesMemoryStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SqliteHermesMemoryStore(
        IOptions<HermesMemoryOptions> options,
        ILogger<SqliteHermesMemoryStore> logger)
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
                CREATE TABLE IF NOT EXISTS memories (
                  id TEXT PRIMARY KEY,
                  run_id TEXT NOT NULL,
                  user_id TEXT,
                  request_fingerprint TEXT NOT NULL,
                  kind INTEGER NOT NULL,
                  stage TEXT NOT NULL,
                  key TEXT NOT NULL,
                  summary TEXT NOT NULL,
                  payload_json TEXT,
                  tokens INTEGER NOT NULL DEFAULT 0,
                  score REAL NOT NULL DEFAULT 0,
                  created_at_utc TEXT NOT NULL,
                  UNIQUE(request_fingerprint, key)
                );
                CREATE INDEX IF NOT EXISTS ix_memories_fingerprint ON memories(request_fingerprint);
                CREATE INDEX IF NOT EXISTS ix_memories_kind_created ON memories(kind, created_at_utc);
                CREATE INDEX IF NOT EXISTS ix_memories_user ON memories(user_id);
                CREATE VIRTUAL TABLE IF NOT EXISTS memory_fts USING fts5(
                  memory_id UNINDEXED,
                  run_id UNINDEXED,
                  kind UNINDEXED,
                  key,
                  summary
                );
                """;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO memories(id, run_id, user_id, request_fingerprint, kind, stage, key, summary, payload_json, tokens, score, created_at_utc)
                VALUES ($id, $run, $user, $fp, $kind, $stage, $key, $summary, $payload, $tokens, $score, $created)
                ON CONFLICT(request_fingerprint, key) DO UPDATE SET
                  run_id = excluded.run_id,
                  user_id = excluded.user_id,
                  kind = excluded.kind,
                  stage = excluded.stage,
                  summary = excluded.summary,
                  payload_json = excluded.payload_json,
                  tokens = excluded.tokens,
                  score = excluded.score,
                  created_at_utc = excluded.created_at_utc
                """;
            BindEntry(cmd, entry);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            var storedId = await ResolveStoredIdAsync(conn, entry.RequestFingerprint, entry.Key, ct).ConfigureAwait(false)
                ?? entry.Id;
            await SyncFtsRowAsync(conn, entry with { Id = storedId }, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HermesMemorySearchHit>> SearchSummariesAsync(string query, int limit = 25, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<HermesMemorySearchHit>();

        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var ftsQuery = FtsQueryHelper.ToMatchExpression(query);
        var hits = new List<HermesMemorySearchHit>();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT run_id, kind, key, snippet(memory_fts, 4, '[', ']', '…', 32)
                FROM memory_fts
                WHERE memory_fts MATCH $q
                LIMIT $lim
                """;
            cmd.Parameters.AddWithValue("$q", ftsQuery);
            cmd.Parameters.AddWithValue("$lim", Math.Max(1, limit));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                hits.Add(new HermesMemorySearchHit(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    1.0));
            }
        }
        finally
        {
            _gate.Release();
        }

        return hits;
    }

    public async Task<IReadOnlyList<HermesMemoryEntry>> ListAllAsync(CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var entries = new List<HermesMemoryEntry>();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, run_id, user_id, request_fingerprint, kind, stage, key, summary, payload_json, tokens, score, created_at_utc
                FROM memories
                ORDER BY created_at_utc DESC
                """;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                entries.Add(ReadEntry(reader));
        }
        finally
        {
            _gate.Release();
        }

        return entries;
    }

    public async Task<int> DeleteByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0)
            return 0;

        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var deleted = 0;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            foreach (var id in ids.Distinct())
            {
                await using var deleteFts = conn.CreateCommand();
                deleteFts.CommandText = "DELETE FROM memory_fts WHERE memory_id = $id";
                deleteFts.Parameters.AddWithValue("$id", id.ToString("D"));
                await deleteFts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                await using var deleteMemory = conn.CreateCommand();
                deleteMemory.CommandText = "DELETE FROM memories WHERE id = $id";
                deleteMemory.Parameters.AddWithValue("$id", id.ToString("D"));
                deleted += await deleteMemory.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }

        return deleted;
    }

    public async Task<IReadOnlyList<HermesMemoryRetrievalResult>> RetrieveAsync(HermesMemoryQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        var entries = new List<HermesMemoryEntry>();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, run_id, user_id, request_fingerprint, kind, stage, key, summary, payload_json, tokens, score, created_at_utc
                FROM memories
                WHERE request_fingerprint = $fp
                """;
            cmd.Parameters.AddWithValue("$fp", query.RequestFingerprint);
            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                cmd.CommandText += " AND (user_id IS NULL OR user_id = $user)";
                cmd.Parameters.AddWithValue("$user", query.UserId);
            }

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                entries.Add(ReadEntry(reader));
        }
        finally
        {
            _gate.Release();
        }

        var filtered = entries
            .Where(entry => query.Kinds is null || query.Kinds.Length == 0 || query.Kinds.Contains(entry.Kind))
            .Where(entry => string.IsNullOrWhiteSpace(query.Keyword)
                || entry.Summary.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase)
                || entry.Key.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase)
                || entry.Stage.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new HermesMemoryRetrievalResult(
                entry,
                HermesMemoryScoring.BuildRetrievalReason(entry, query.Keyword),
                HermesMemoryScoring.ComputeRelevanceScore(entry, query.Keyword)))
            .OrderByDescending(result => result.RelevanceScore)
            .ThenByDescending(result => result.Entry.CreatedAtUtc)
            .Take(Math.Max(0, query.TopK))
            .ToList();

        return filtered;
    }

    public async Task<int> PruneExpiredEpisodicAsync(CancellationToken ct = default)
    {
        if (!_options.EnableRetentionPrune || _options.EpisodicRetentionDays <= 0)
            return 0;

        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var cutoff = DateTime.UtcNow.AddDays(-_options.EpisodicRetentionDays).ToString("O");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                DELETE FROM memories
                WHERE kind = $episodic AND created_at_utc < $cutoff
                """;
            cmd.Parameters.AddWithValue("$episodic", (int)MemoryKind.Episodic);
            cmd.Parameters.AddWithValue("$cutoff", cutoff);
            var deleted = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            if (deleted > 0)
                _logger.LogInformation("Pruned {Count} expired episodic memories (retention {Days}d)", deleted, _options.EpisodicRetentionDays);
            return deleted;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task PruneByTokenBudgetAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(requestFingerprint))
            return;

        if (maxTokenBudget <= 0)
        {
            await DeleteFingerprintAsync(requestFingerprint, ct).ConfigureAwait(false);
            return;
        }

        var entries = (await RetrieveAsync(new HermesMemoryQuery(requestFingerprint, TopK: 10_000), ct).ConfigureAwait(false))
            .Select(r => r.Entry)
            .OrderByDescending(entry => HermesMemoryScoring.ComputeRelevanceScore(entry, keyword: null))
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .ToList();

        var kept = new List<HermesMemoryEntry>();
        var budgetUsed = 0;
        foreach (var entry in entries)
        {
            if (budgetUsed + entry.Tokens > maxTokenBudget)
                continue;
            kept.Add(entry);
            budgetUsed += entry.Tokens;
        }

        var keptKeys = kept.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
        var toDelete = entries.Where(e => !keptKeys.Contains(e.Key)).Select(e => e.Key).ToList();
        if (toDelete.Count == 0)
            return;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            foreach (var key in toDelete)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM memories WHERE request_fingerprint = $fp AND key = $key";
                cmd.Parameters.AddWithValue("$fp", requestFingerprint);
                cmd.Parameters.AddWithValue("$key", key);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    Task IMemoryStore.IngestAsync(MemoryRecord record, CancellationToken ct) =>
        UpsertAsync(ToHermesEntry(record), ct);

    async Task<IReadOnlyList<MemoryRetrievalResult>> IMemoryStore.RetrieveAsync(MemoryQuery query, CancellationToken ct)
    {
        var results = await RetrieveAsync(
            new HermesMemoryQuery(query.RequestFingerprint, query.Keyword, query.TopK, query.Kinds),
            ct).ConfigureAwait(false);

        return results
            .Select(r => new MemoryRetrievalResult(ToMemoryRecord(r.Entry), r.RetrievalReason, r.RelevanceScore))
            .ToList();
    }

    Task IMemoryStore.PruneAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct) =>
        PruneByTokenBudgetAsync(requestFingerprint, maxTokenBudget, ct);

    private async Task DeleteFingerprintAsync(string requestFingerprint, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var conn = Open();
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM memories WHERE request_fingerprint = $fp";
            cmd.Parameters.AddWithValue("$fp", requestFingerprint);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<Guid?> ResolveStoredIdAsync(
        SqliteConnection conn,
        string requestFingerprint,
        string key,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM memories WHERE request_fingerprint = $fp AND key = $key LIMIT 1";
        cmd.Parameters.AddWithValue("$fp", requestFingerprint);
        cmd.Parameters.AddWithValue("$key", key);
        var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return value is string id && Guid.TryParse(id, out var parsed) ? parsed : null;
    }

    private static async Task SyncFtsRowAsync(SqliteConnection conn, HermesMemoryEntry entry, CancellationToken ct)
    {
        await using var delete = conn.CreateCommand();
        delete.CommandText = "DELETE FROM memory_fts WHERE memory_id = $id";
        delete.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
        await delete.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        await using var insert = conn.CreateCommand();
        insert.CommandText = """
            INSERT INTO memory_fts(memory_id, run_id, kind, key, summary)
            VALUES ($id, $run, $kind, $key, $summary)
            """;
        insert.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
        insert.Parameters.AddWithValue("$run", entry.RunId.ToString("D"));
        insert.Parameters.AddWithValue("$kind", entry.Kind.ToString());
        insert.Parameters.AddWithValue("$key", entry.Key);
        insert.Parameters.AddWithValue("$summary", entry.Summary);
        await insert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private SqliteConnection Open() => new(_connectionString);

    private static void BindEntry(SqliteCommand cmd, HermesMemoryEntry entry)
    {
        cmd.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
        cmd.Parameters.AddWithValue("$run", entry.RunId.ToString("D"));
        cmd.Parameters.AddWithValue("$user", (object?)entry.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$fp", entry.RequestFingerprint);
        cmd.Parameters.AddWithValue("$kind", (int)entry.Kind);
        cmd.Parameters.AddWithValue("$stage", entry.Stage);
        cmd.Parameters.AddWithValue("$key", entry.Key);
        cmd.Parameters.AddWithValue("$summary", entry.Summary);
        cmd.Parameters.AddWithValue("$payload", (object?)entry.PayloadJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tokens", entry.Tokens);
        cmd.Parameters.AddWithValue("$score", entry.Score);
        cmd.Parameters.AddWithValue("$created", entry.CreatedAtUtc.ToString("O"));
    }

    private static HermesMemoryEntry ReadEntry(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            (MemoryKind)reader.GetInt32(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetInt32(9),
            reader.GetDouble(10),
            DateTime.Parse(reader.GetString(11), null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static HermesMemoryEntry ToHermesEntry(MemoryRecord record) =>
        new(
            Id: Guid.NewGuid(),
            RunId: record.RunId,
            UserId: null,
            RequestFingerprint: record.RequestFingerprint,
            Kind: record.Kind,
            Stage: record.Stage,
            Key: record.Key,
            Summary: record.Summary,
            PayloadJson: record.PayloadJson,
            Tokens: record.TokenEstimate,
            Score: 0,
            CreatedAtUtc: record.CreatedAtUtc);

    private static MemoryRecord ToMemoryRecord(HermesMemoryEntry entry) =>
        new(
            entry.RunId,
            entry.RequestFingerprint,
            entry.Stage,
            entry.Kind,
            entry.Key,
            entry.Summary,
            entry.PayloadJson,
            entry.Tokens,
            entry.CreatedAtUtc);
}
