using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunSessionSnapshotImporter
{
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<RunSessionSnapshotImporter> _logger;

    public RunSessionSnapshotImporter(
        IOptions<AgentRuntimeOptions> options,
        ILogger<RunSessionSnapshotImporter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ImportAsync(
        Guid newRunId,
        string snapshotPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(snapshotPath))
            return 0;

        var destination = Path.GetFullPath(_options.SessionDbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = snapshotPath }.ConnectionString);
        await source.OpenAsync(ct).ConfigureAwait(false);

        var isNew = !File.Exists(destination);
        await using var dest = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination }.ConnectionString);
        await dest.OpenAsync(ct).ConfigureAwait(false);

        if (isNew)
            await EnsureSchemaAsync(dest, ct).ConfigureAwait(false);

        var sessionIds = await ReadSessionIdsAsync(source, ct).ConfigureAwait(false);
        var imported = 0;
        var runKey = newRunId.ToString("D");

        foreach (var sessionId in sessionIds)
        {
            await CopySessionWithRunIdAsync(source, dest, sessionId, runKey, ct).ConfigureAwait(false);
            imported++;
        }

        _logger.LogInformation(
            "Imported {Count} agent sessions for run {RunId} from snapshot",
            imported,
            newRunId);

        return imported;
    }

    private static async Task<List<string>> ReadSessionIdsAsync(SqliteConnection source, CancellationToken ct)
    {
        var ids = new List<string>();
        await using var cmd = source.CreateCommand();
        cmd.CommandText = "SELECT session_id FROM agent_sessions";
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task CopySessionWithRunIdAsync(
        SqliteConnection source,
        SqliteConnection dest,
        string sessionId,
        string runKey,
        CancellationToken ct)
    {
        await CopyRowWithRunOverrideAsync(source, dest, "agent_sessions", "session_id", sessionId, runKey, ct)
            .ConfigureAwait(false);
        await CopyRowsAsync(source, dest, "agent_messages", "session_id", sessionId, ct).ConfigureAwait(false);
        await CopyRowsAsync(source, dest, "agent_tool_calls", "session_id", sessionId, ct).ConfigureAwait(false);
        await CopyRowsAsync(source, dest, "agent_checkpoints", "session_id", sessionId, ct).ConfigureAwait(false);
    }

    private static async Task CopyRowWithRunOverrideAsync(
        SqliteConnection source,
        SqliteConnection dest,
        string table,
        string keyColumn,
        string keyValue,
        string runKey,
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
            var column = columns[i];
            object value = reader.GetValue(i);
            if (string.Equals(column, "run_id", StringComparison.OrdinalIgnoreCase))
                value = runKey;
            write.Parameters.AddWithValue($"${column}", value is DBNull ? DBNull.Value : value);
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

        var columns = new List<string>();
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
}
