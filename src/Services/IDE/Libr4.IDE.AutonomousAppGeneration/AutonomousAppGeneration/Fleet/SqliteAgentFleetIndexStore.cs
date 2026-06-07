using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IAgentFleetIndexStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task UpsertAsync(AgentFleetEntry entry, CancellationToken ct = default);
    Task<AgentFleetEntry?> GetAsync(Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentFleetEntry>> ListAsync(AgentFleetListQuery query, CancellationToken ct = default);
    Task PatchAsync(Guid runId, AgentFleetPatchRequest patch, CancellationToken ct = default);
    Task DeleteAsync(Guid runId, CancellationToken ct = default);
}

public sealed class SqliteAgentFleetIndexStore : IAgentFleetIndexStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteAgentFleetIndexStore> _logger;

    public SqliteAgentFleetIndexStore(IOptions<AgentFleetOptions> options, ILogger<SqliteAgentFleetIndexStore> logger)
    {
        var path = options.Value.IndexDbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS agent_fleet_index (
              run_id TEXT PRIMARY KEY,
              title TEXT NOT NULL,
              space_id TEXT,
              status TEXT NOT NULL,
              stage TEXT NOT NULL,
              agent_count INTEGER NOT NULL DEFAULT 0,
              started_at_utc TEXT NOT NULL,
              last_activity_at_utc TEXT NOT NULL,
              cost_usd REAL NOT NULL DEFAULT 0,
              model_profile TEXT,
              verify_status TEXT,
              stack TEXT,
              pinned INTEGER NOT NULL DEFAULT 0,
              archived INTEGER NOT NULL DEFAULT 0,
              failure_reason TEXT
            );
            CREATE INDEX IF NOT EXISTS ix_agent_fleet_status ON agent_fleet_index(status, last_activity_at_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_agent_fleet_archived ON agent_fleet_index(archived, last_activity_at_utc DESC);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await MigrateSchemaAsync(conn, ct).ConfigureAwait(false);
    }

    private static async Task MigrateSchemaAsync(SqliteConnection conn, CancellationToken ct)
    {
        await EnsureColumnAsync(conn, "pr_url", "TEXT", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "pr_number", "INTEGER", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "ci_status", "TEXT", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "ci_logs_url", "TEXT", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "playbook_hits", "INTEGER", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "playbook_attempts", "INTEGER", ct).ConfigureAwait(false);
        await EnsureColumnAsync(conn, "quality_score", "INTEGER", ct).ConfigureAwait(false);
    }

    private static async Task EnsureColumnAsync(SqliteConnection conn, string column, string type, CancellationToken ct)
    {
        await using var info = conn.CreateCommand();
        info.CommandText = "PRAGMA table_info(agent_fleet_index)";
        await using var reader = await info.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                return;
        }

        await using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE agent_fleet_index ADD COLUMN {column} {type}";
        await alter.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task UpsertAsync(AgentFleetEntry entry, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_fleet_index(
              run_id, title, space_id, status, stage, agent_count,
              started_at_utc, last_activity_at_utc, cost_usd, model_profile,
              verify_status, stack, pinned, archived, failure_reason,
              pr_url, pr_number, ci_status, ci_logs_url, playbook_hits, playbook_attempts, quality_score)
            VALUES ($run, $title, $space, $status, $stage, $agents,
                    $started, $last, $cost, $model, $verify, $stack, $pinned, $archived, $fail,
                    $prUrl, $prNumber, $ciStatus, $ciLogsUrl, $playbookHits, $playbookAttempts, $qualityScore)
            ON CONFLICT(run_id) DO UPDATE SET
              title = excluded.title,
              space_id = excluded.space_id,
              status = excluded.status,
              stage = excluded.stage,
              agent_count = excluded.agent_count,
              last_activity_at_utc = excluded.last_activity_at_utc,
              cost_usd = excluded.cost_usd,
              model_profile = excluded.model_profile,
              verify_status = excluded.verify_status,
              stack = excluded.stack,
              pinned = excluded.pinned,
              archived = excluded.archived,
              failure_reason = excluded.failure_reason,
              pr_url = excluded.pr_url,
              pr_number = excluded.pr_number,
              ci_status = excluded.ci_status,
              ci_logs_url = excluded.ci_logs_url,
              playbook_hits = excluded.playbook_hits,
              playbook_attempts = excluded.playbook_attempts,
              quality_score = excluded.quality_score
            """;
        BindEntry(cmd, entry);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<AgentFleetEntry?> GetAsync(Guid runId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_fleet_index WHERE run_id = $run";
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadEntry(reader) : null;
    }

    public async Task<IReadOnlyList<AgentFleetEntry>> ListAsync(AgentFleetListQuery query, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();

        var filters = new List<string>();
        if (!query.IncludeArchived)
            filters.Add("archived = 0");
        if (query.Status is { } status)
            filters.Add("status = $status");
        if (!string.IsNullOrWhiteSpace(query.SpaceId))
            filters.Add("space_id = $space");
        if (!string.IsNullOrWhiteSpace(query.Stack))
            filters.Add("stack = $stack");
        if (!string.IsNullOrWhiteSpace(query.Search))
            filters.Add("(title LIKE $search OR run_id LIKE $search)");

        var where = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : string.Empty;
        var orderBy = string.Equals(query.SortBy, "quality", StringComparison.OrdinalIgnoreCase)
            ? "ORDER BY pinned DESC, quality_score DESC, last_activity_at_utc DESC"
            : "ORDER BY pinned DESC, last_activity_at_utc DESC";
        cmd.CommandText = $"""
            SELECT * FROM agent_fleet_index
            {where}
            {orderBy}
            LIMIT $limit
            """;

        if (query.Status is { } s)
            cmd.Parameters.AddWithValue("$status", s.ToString());
        if (!string.IsNullOrWhiteSpace(query.SpaceId))
            cmd.Parameters.AddWithValue("$space", query.SpaceId);
        if (!string.IsNullOrWhiteSpace(query.Stack))
            cmd.Parameters.AddWithValue("$stack", query.Stack);
        if (!string.IsNullOrWhiteSpace(query.Search))
            cmd.Parameters.AddWithValue("$search", $"%{query.Search.Trim()}%");
        cmd.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 1, 500));

        var results = new List<AgentFleetEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            results.Add(ReadEntry(reader));
        return results;
    }

    public async Task PatchAsync(Guid runId, AgentFleetPatchRequest patch, CancellationToken ct = default)
    {
        var existing = await GetAsync(runId, ct).ConfigureAwait(false);
        if (existing is null)
            return;

        var updated = existing with
        {
            Title = patch.Title ?? existing.Title,
            Pinned = patch.Pinned ?? existing.Pinned,
            Archived = patch.Archived ?? existing.Archived,
            LastActivityAtUtc = DateTime.UtcNow
        };
        await UpsertAsync(updated, ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid runId, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM agent_fleet_index WHERE run_id = $run", runId, ct);

    private async Task ExecuteAsync(string sql, Guid runId, CancellationToken ct)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static void BindEntry(SqliteCommand cmd, AgentFleetEntry entry)
    {
        cmd.Parameters.AddWithValue("$run", entry.RunId.ToString("D"));
        cmd.Parameters.AddWithValue("$title", entry.Title);
        cmd.Parameters.AddWithValue("$space", (object?)entry.SpaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", entry.Status.ToString());
        cmd.Parameters.AddWithValue("$stage", entry.Stage);
        cmd.Parameters.AddWithValue("$agents", entry.AgentCount);
        cmd.Parameters.AddWithValue("$started", entry.StartedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$last", entry.LastActivityAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$cost", entry.CostUsd);
        cmd.Parameters.AddWithValue("$model", (object?)entry.ModelProfile ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$verify", (object?)entry.VerifyStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$stack", (object?)entry.Stack ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pinned", entry.Pinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$archived", entry.Archived ? 1 : 0);
        cmd.Parameters.AddWithValue("$fail", (object?)entry.FailureReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prUrl", (object?)entry.PrUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prNumber", (object?)entry.PrNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ciStatus", (object?)entry.CiStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ciLogsUrl", (object?)entry.CiLogsUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$playbookHits", entry.PlaybookHits);
        cmd.Parameters.AddWithValue("$playbookAttempts", entry.PlaybookAttempts);
        cmd.Parameters.AddWithValue("$qualityScore", entry.QualityScore);
    }

    private static string? ReadOptionalString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? ReadOptionalInt(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static AgentFleetEntry ReadEntry(SqliteDataReader reader) =>
        new(
            RunId: Guid.Parse(reader.GetString(reader.GetOrdinal("run_id"))),
            Title: reader.GetString(reader.GetOrdinal("title")),
            SpaceId: reader.IsDBNull(reader.GetOrdinal("space_id")) ? null : reader.GetString(reader.GetOrdinal("space_id")),
            Status: Enum.Parse<AgentFleetStatus>(reader.GetString(reader.GetOrdinal("status"))),
            Stage: reader.GetString(reader.GetOrdinal("stage")),
            AgentCount: reader.GetInt32(reader.GetOrdinal("agent_count")),
            StartedAtUtc: DateTime.Parse(reader.GetString(reader.GetOrdinal("started_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            LastActivityAtUtc: DateTime.Parse(reader.GetString(reader.GetOrdinal("last_activity_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            CostUsd: reader.GetDouble(reader.GetOrdinal("cost_usd")),
            ModelProfile: reader.IsDBNull(reader.GetOrdinal("model_profile")) ? null : reader.GetString(reader.GetOrdinal("model_profile")),
            VerifyStatus: reader.IsDBNull(reader.GetOrdinal("verify_status")) ? null : reader.GetString(reader.GetOrdinal("verify_status")),
            Stack: reader.IsDBNull(reader.GetOrdinal("stack")) ? null : reader.GetString(reader.GetOrdinal("stack")),
            Pinned: reader.GetInt32(reader.GetOrdinal("pinned")) == 1,
            Archived: reader.GetInt32(reader.GetOrdinal("archived")) == 1,
            FailureReason: reader.IsDBNull(reader.GetOrdinal("failure_reason")) ? null : reader.GetString(reader.GetOrdinal("failure_reason")),
            PrUrl: ReadOptionalString(reader, "pr_url"),
            PrNumber: ReadOptionalInt(reader, "pr_number"),
            CiStatus: ReadOptionalString(reader, "ci_status"),
            CiLogsUrl: ReadOptionalString(reader, "ci_logs_url"),
            PlaybookHits: ReadOptionalInt(reader, "playbook_hits") ?? 0,
            PlaybookAttempts: ReadOptionalInt(reader, "playbook_attempts") ?? 0,
            QualityScore: ReadOptionalInt(reader, "quality_score") ?? 0);

    private SqliteConnection Open() => new(_connectionString);
}
