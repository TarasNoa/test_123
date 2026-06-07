using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;

public sealed class SqliteAgentSessionStore : IAgentSessionStore, IAgentSessionResumeService
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteAgentSessionStore> _logger;

    public SqliteAgentSessionStore(IOptions<AgentRuntimeOptions> options, ILogger<SqliteAgentSessionStore> logger)
    {
        var path = options.Value.SessionDbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        var sql = """
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
              timestamp_utc TEXT NOT NULL,
              FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id)
            );
            CREATE INDEX IF NOT EXISTS ix_agent_messages_session ON agent_messages(session_id, step_number);
            CREATE TABLE IF NOT EXISTS agent_tool_calls (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              session_id TEXT NOT NULL,
              tool_name TEXT NOT NULL,
              input_json TEXT NOT NULL,
              output_json TEXT,
              success INTEGER NOT NULL,
              duration_ms INTEGER NOT NULL,
              started_at_utc TEXT NOT NULL,
              FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id)
            );
            CREATE TABLE IF NOT EXISTS agent_checkpoints (
              checkpoint_id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              step_number INTEGER NOT NULL,
              messages_json TEXT NOT NULL,
              file_hashes_json TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id)
            );
            """;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<AgentSessionRecord> CreateSessionAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_sessions(session_id, run_id, subagent_id, model, status, created_at_utc, last_step_at_utc, token_budget, cost_usd, permission_mode, current_step_number)
            VALUES ($id, $run, $sub, $model, $status, $created, $last, $budget, $cost, $perm, $step)
            """;
        BindSession(cmd, session);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return session;
    }

    public async Task<AgentSessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_sessions WHERE session_id = $id";
        cmd.Parameters.AddWithValue("$id", sessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadSession(reader) : null;
    }

    public async Task UpdateSessionAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE agent_sessions SET run_id=$run, subagent_id=$sub, model=$model, status=$status, last_step_at_utc=$last,
              token_budget=$budget, cost_usd=$cost, permission_mode=$perm, current_step_number=$step
            WHERE session_id=$id
            """;
        BindSession(cmd, session);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task AppendMessageAsync(AgentMessageRecord message, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_messages(session_id, role, content, tool_calls_json, step_number, timestamp_utc)
            VALUES ($sid, $role, $content, $tools, $step, $ts)
            """;
        cmd.Parameters.AddWithValue("$sid", message.SessionId);
        cmd.Parameters.AddWithValue("$role", message.Role);
        cmd.Parameters.AddWithValue("$content", message.Content);
        cmd.Parameters.AddWithValue("$tools", (object?)message.ToolCallsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$step", message.StepNumber);
        cmd.Parameters.AddWithValue("$ts", message.TimestampUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AgentMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken ct = default)
    {
        var list = new List<AgentMessageRecord>();
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_messages WHERE session_id = $id ORDER BY step_number, id";
        cmd.Parameters.AddWithValue("$id", sessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(new AgentMessageRecord(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5),
                DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)));
        }

        return list;
    }

    public async Task AppendToolCallAsync(AgentToolCallRecord toolCall, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_tool_calls(session_id, tool_name, input_json, output_json, success, duration_ms, started_at_utc)
            VALUES ($sid, $tool, $in, $out, $ok, $dur, $ts)
            """;
        cmd.Parameters.AddWithValue("$sid", toolCall.SessionId);
        cmd.Parameters.AddWithValue("$tool", toolCall.ToolName);
        cmd.Parameters.AddWithValue("$in", toolCall.InputJson);
        cmd.Parameters.AddWithValue("$out", (object?)toolCall.OutputJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ok", toolCall.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$dur", toolCall.DurationMs);
        cmd.Parameters.AddWithValue("$ts", toolCall.StartedAtUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveCheckpointAsync(AgentCheckpointRecord checkpoint, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO agent_checkpoints(checkpoint_id, session_id, step_number, messages_json, file_hashes_json, created_at_utc)
            VALUES ($id, $sid, $step, $msgs, $hashes, $ts)
            """;
        cmd.Parameters.AddWithValue("$id", checkpoint.CheckpointId);
        cmd.Parameters.AddWithValue("$sid", checkpoint.SessionId);
        cmd.Parameters.AddWithValue("$step", checkpoint.StepNumber);
        cmd.Parameters.AddWithValue("$msgs", checkpoint.MessagesJson);
        cmd.Parameters.AddWithValue("$hashes", checkpoint.FileHashesJson);
        cmd.Parameters.AddWithValue("$ts", checkpoint.CreatedAtUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<AgentCheckpointRecord?> GetCheckpointAsync(string checkpointId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_checkpoints WHERE checkpoint_id = $id";
        cmd.Parameters.AddWithValue("$id", checkpointId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;
        return new AgentCheckpointRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public async Task<IReadOnlyList<AgentCheckpointRecord>> ListCheckpointsAsync(string sessionId, CancellationToken ct = default)
    {
        var list = new List<AgentCheckpointRecord>();
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_checkpoints WHERE session_id = $id ORDER BY step_number";
        cmd.Parameters.AddWithValue("$id", sessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(new AgentCheckpointRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind)));
        }

        return list;
    }

    public async Task<AgentSessionRecord?> GetLatestSessionByRunIdAsync(Guid runId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM agent_sessions
            WHERE run_id = $run
            ORDER BY last_step_at_utc DESC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadSession(reader) : null;
    }

    public async Task<IReadOnlyList<AgentToolCallRecord>> GetToolCallsAsync(string sessionId, CancellationToken ct = default)
    {
        var list = new List<AgentToolCallRecord>();
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_tool_calls WHERE session_id = $id ORDER BY started_at_utc, id";
        cmd.Parameters.AddWithValue("$id", sessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(new AgentToolCallRecord(
                reader.GetInt64(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("session_id")),
                reader.GetString(reader.GetOrdinal("tool_name")),
                reader.GetString(reader.GetOrdinal("input_json")),
                reader.IsDBNull(reader.GetOrdinal("output_json")) ? null : reader.GetString(reader.GetOrdinal("output_json")),
                reader.GetInt32(reader.GetOrdinal("success")) != 0,
                reader.GetInt64(reader.GetOrdinal("duration_ms")),
                DateTime.Parse(reader.GetString(reader.GetOrdinal("started_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind)));
        }

        return list;
    }

    public async Task<AgentSessionResumeBundle?> LoadResumeBundleAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct).ConfigureAwait(false);
        if (session is null)
            return null;
        var messages = await GetMessagesAsync(sessionId, ct).ConfigureAwait(false);
        var turns = messages.Select(m => new AgentConversationTurn(m.Role, m.Content, m.TimestampUtc)).ToList();
        return new AgentSessionResumeBundle(session, turns, session.CurrentStepNumber + 1);
    }

    public async Task SaveTurnAsync(
        string sessionId,
        int stepNumber,
        AgentConversationTurn turn,
        AgentToolCallRecord? toolCall,
        CancellationToken ct = default)
    {
        await AppendMessageAsync(new AgentMessageRecord(
            0, sessionId, turn.Role, turn.Content, null, stepNumber, turn.AtUtc), ct).ConfigureAwait(false);
        if (toolCall is not null)
            await AppendToolCallAsync(toolCall, ct).ConfigureAwait(false);

        var session = await GetSessionAsync(sessionId, ct).ConfigureAwait(false);
        if (session is null)
            return;

        await UpdateSessionAsync(session with
        {
            LastStepAtUtc = DateTime.UtcNow,
            CurrentStepNumber = stepNumber
        }, ct).ConfigureAwait(false);
    }

    private SqliteConnection Open() => new(_connectionString);

    private static void BindSession(SqliteCommand cmd, AgentSessionRecord session)
    {
        cmd.Parameters.AddWithValue("$id", session.SessionId);
        cmd.Parameters.AddWithValue("$run", session.RunId?.ToString("D") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$sub", (object?)session.SubagentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$model", (object?)session.Model ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", session.Status);
        cmd.Parameters.AddWithValue("$created", session.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$last", session.LastStepAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$budget", session.TokenBudget);
        cmd.Parameters.AddWithValue("$cost", session.CostUsd);
        cmd.Parameters.AddWithValue("$perm", session.PermissionMode);
        cmd.Parameters.AddWithValue("$step", session.CurrentStepNumber);
    }

    private static AgentSessionRecord ReadSession(SqliteDataReader reader) =>
        new(
            reader.GetString(reader.GetOrdinal("session_id")),
            reader.IsDBNull(reader.GetOrdinal("run_id")) ? null : Guid.Parse(reader.GetString(reader.GetOrdinal("run_id"))),
            reader.IsDBNull(reader.GetOrdinal("subagent_id")) ? null : reader.GetString(reader.GetOrdinal("subagent_id")),
            reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString(reader.GetOrdinal("model")),
            reader.GetString(reader.GetOrdinal("status")),
            DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(reader.GetString(reader.GetOrdinal("last_step_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.GetInt32(reader.GetOrdinal("token_budget")),
            reader.GetDouble(reader.GetOrdinal("cost_usd")),
            reader.GetString(reader.GetOrdinal("permission_mode")),
            reader.GetInt32(reader.GetOrdinal("current_step_number")));
}
