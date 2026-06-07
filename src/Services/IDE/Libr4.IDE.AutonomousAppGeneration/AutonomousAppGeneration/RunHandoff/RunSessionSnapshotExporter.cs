using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunSessionSnapshotExporter
{
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<RunSessionSnapshotExporter> _logger;

    public RunSessionSnapshotExporter(
        IOptions<AgentRuntimeOptions> options,
        ILogger<RunSessionSnapshotExporter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ExportRunSessionsAsync(Guid runId, string destinationPath, CancellationToken ct = default)
    {
        var sourcePath = Path.GetFullPath(_options.SessionDbPath);
        if (!File.Exists(sourcePath))
        {
            _logger.LogDebug("Session database not found at {Path}; skipping session snapshot", sourcePath);
            return false;
        }

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        var runKey = runId.ToString("D");
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sourcePath }.ConnectionString);
        await source.OpenAsync(ct).ConfigureAwait(false);

        await using var dest = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destinationPath }.ConnectionString);
        await dest.OpenAsync(ct).ConfigureAwait(false);

        await EnsureSchemaAsync(dest, ct).ConfigureAwait(false);

        var sessionIds = await ReadSessionIdsAsync(source, runKey, ct).ConfigureAwait(false);
        if (sessionIds.Count == 0)
            return false;

        foreach (var sessionId in sessionIds)
        {
            await CopyRowAsync(
                source,
                dest,
                "agent_sessions",
                "session_id",
                sessionId,
                ct).ConfigureAwait(false);
            await CopyRowsAsync(
                source,
                dest,
                "agent_messages",
                "session_id",
                sessionId,
                ct).ConfigureAwait(false);
            await CopyRowsAsync(
                source,
                dest,
                "agent_tool_calls",
                "session_id",
                sessionId,
                ct).ConfigureAwait(false);
            await CopyRowsAsync(
                source,
                dest,
                "agent_checkpoints",
                "session_id",
                sessionId,
                ct).ConfigureAwait(false);
        }

        return true;
    }

    private static async Task<List<string>> ReadSessionIdsAsync(
        SqliteConnection source,
        string runKey,
        CancellationToken ct)
    {
        var ids = new List<string>();
        await using var cmd = source.CreateCommand();
        cmd.CommandText = "SELECT session_id FROM agent_sessions WHERE run_id = $run";
        cmd.Parameters.AddWithValue("$run", runKey);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection dest, CancellationToken ct)
    {
        await using var cmd = dest.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS agent_sessions (
              session_id TEXT PRIMARY KEY,
              run_id TEXT,
              subagent_id TEXT,
              model TEXT,
              status TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              last_step_at_utc TEXT NOT NULL,
              token_budget INTEGER NOT NULL DEFAULT 0,
              cost_usd REAL NOT NULL DEFAULT 0,
              permission_mode TEXT NOT NULL DEFAULT 'BypassPermissions',
              current_step_number INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS agent_messages (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              session_id TEXT NOT NULL,
              role TEXT NOT NULL,
              content TEXT NOT NULL,
              tool_calls_json TEXT,
              step_number INTEGER NOT NULL,
              timestamp_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS agent_tool_calls (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              session_id TEXT NOT NULL,
              tool_name TEXT NOT NULL,
              input_json TEXT NOT NULL,
              output_json TEXT,
              success INTEGER NOT NULL,
              duration_ms INTEGER NOT NULL,
              started_at_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS agent_checkpoints (
              checkpoint_id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              step_number INTEGER NOT NULL,
              messages_json TEXT NOT NULL,
              file_hashes_json TEXT NOT NULL,
              created_at_utc TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task CopyRowAsync(
        SqliteConnection source,
        SqliteConnection dest,
        string table,
        string keyColumn,
        string keyValue,
        CancellationToken ct)
    {
        await using var read = source.CreateCommand();
        read.CommandText = $"SELECT * FROM {table} WHERE {keyColumn} = $key LIMIT 1";
        read.Parameters.AddWithValue("$key", keyValue);
        await using var reader = await read.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return;

        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var placeholders = string.Join(", ", columns.Select(c => $"${c}"));
        var columnList = string.Join(", ", columns);

        await using var write = dest.CreateCommand();
        write.CommandText = $"INSERT OR REPLACE INTO {table} ({columnList}) VALUES ({placeholders})";
        for (var i = 0; i < columns.Count; i++)
        {
            var value = reader.GetValue(i);
            write.Parameters.AddWithValue($"${columns[i]}", value is DBNull ? DBNull.Value : value);
        }

        await write.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task CopyRowsAsync(
        SqliteConnection source,
        SqliteConnection dest,
        string table,
        string keyColumn,
        string keyValue,
        CancellationToken ct)
    {
        await using var read = source.CreateCommand();
        read.CommandText = $"SELECT * FROM {table} WHERE {keyColumn} = $key";
        read.Parameters.AddWithValue("$key", keyValue);
        await using var reader = await read.ExecuteReaderAsync(ct).ConfigureAwait(false);

        var columns = reader.FieldCount > 0
            ? Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList()
            : new List<string>();

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (columns.Count == 0)
                columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            var placeholders = string.Join(", ", columns.Select(c => $"${c}"));
            var columnList = string.Join(", ", columns);

            await using var write = dest.CreateCommand();
            write.CommandText = $"INSERT OR REPLACE INTO {table} ({columnList}) VALUES ({placeholders})";
            for (var i = 0; i < columns.Count; i++)
            {
                var value = reader.GetValue(i);
                write.Parameters.AddWithValue($"${columns[i]}", value is DBNull ? DBNull.Value : value);
            }

            await write.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
