using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public sealed class SqliteScheduledAgentRunStore : IScheduledAgentRunStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteScheduledAgentRunStore> _logger;

    public SqliteScheduledAgentRunStore(IOptions<AgentSchedulingOptions> options, ILogger<SqliteScheduledAgentRunStore> logger)
    {
        var path = options.Value.DbPath;
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
            CREATE TABLE IF NOT EXISTS scheduled_agent_runs (
              schedule_id TEXT PRIMARY KEY,
              flow_name TEXT NOT NULL,
              cron_expression TEXT NOT NULL,
              user_request TEXT NOT NULL,
              max_iterations INTEGER NOT NULL DEFAULT 8,
              enabled INTEGER NOT NULL DEFAULT 1,
              tenant_id TEXT,
              last_run_at_utc TEXT,
              last_run_id TEXT
            );
            CREATE INDEX IF NOT EXISTS ix_scheduled_agent_runs_flow ON scheduled_agent_runs(flow_name);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ScheduledAgentRunDefinition>> ListAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM scheduled_agent_runs ORDER BY flow_name";
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var list = new List<ScheduledAgentRunDefinition>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(Read(reader));
        return list;
    }

    public async Task UpsertAsync(ScheduledAgentRunDefinition definition, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO scheduled_agent_runs(
              schedule_id, flow_name, cron_expression, user_request, max_iterations, enabled, tenant_id, last_run_at_utc, last_run_id)
            VALUES ($id, $flow, $cron, $req, $max, $enabled, $tenant, $last, $run)
            ON CONFLICT(schedule_id) DO UPDATE SET
              flow_name = excluded.flow_name,
              cron_expression = excluded.cron_expression,
              user_request = excluded.user_request,
              max_iterations = excluded.max_iterations,
              enabled = excluded.enabled,
              tenant_id = excluded.tenant_id
            """;
        Bind(cmd, definition);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string scheduleId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM scheduled_agent_runs WHERE schedule_id = $id";
        cmd.Parameters.AddWithValue("$id", scheduleId);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordExecutionAsync(string scheduleId, Guid runId, DateTime executedAtUtc, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE scheduled_agent_runs
            SET last_run_at_utc = $last, last_run_id = $run
            WHERE schedule_id = $id
            """;
        cmd.Parameters.AddWithValue("$id", scheduleId);
        cmd.Parameters.AddWithValue("$last", executedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Recorded scheduled run {ScheduleId} -> {RunId}", scheduleId, runId);
    }

    private static ScheduledAgentRunDefinition Read(SqliteDataReader reader) =>
        new(
            reader.GetString(reader.GetOrdinal("schedule_id")),
            reader.GetString(reader.GetOrdinal("flow_name")),
            reader.GetString(reader.GetOrdinal("cron_expression")),
            reader.GetString(reader.GetOrdinal("user_request")),
            reader.GetInt32(reader.GetOrdinal("max_iterations")),
            reader.GetInt32(reader.GetOrdinal("enabled")) == 1,
            reader.IsDBNull(reader.GetOrdinal("tenant_id")) ? null : reader.GetString(reader.GetOrdinal("tenant_id")),
            reader.IsDBNull(reader.GetOrdinal("last_run_at_utc"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_run_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(reader.GetOrdinal("last_run_id"))
                ? null
                : Guid.Parse(reader.GetString(reader.GetOrdinal("last_run_id"))));

    private static void Bind(SqliteCommand cmd, ScheduledAgentRunDefinition definition)
    {
        cmd.Parameters.AddWithValue("$id", definition.ScheduleId);
        cmd.Parameters.AddWithValue("$flow", definition.FlowName);
        cmd.Parameters.AddWithValue("$cron", definition.CronExpression);
        cmd.Parameters.AddWithValue("$req", definition.UserRequest);
        cmd.Parameters.AddWithValue("$max", definition.MaxIterations);
        cmd.Parameters.AddWithValue("$enabled", definition.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$tenant", (object?)definition.TenantId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$last", definition.LastRunAtUtc?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$run", definition.LastRunId?.ToString("D") ?? (object)DBNull.Value);
    }

    private SqliteConnection Open() => new(_connectionString);
}
