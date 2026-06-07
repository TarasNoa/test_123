using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;

public sealed class FileRolloutRecorder : IRolloutRecorder, IRolloutReplayService
{
    private readonly AgentRuntimeOptions _options;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, object> _runLocks = new();
    private readonly ILogger<FileRolloutRecorder> _logger;

    public FileRolloutRecorder(IOptions<AgentRuntimeOptions> options, ILogger<FileRolloutRecorder>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? NullLogger<FileRolloutRecorder>.Instance;
    }

    public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) =>
        AppendAsync(runId, new { type = "step_start", sessionId, stepNumber, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct);

    public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) =>
        AppendAsync(runId, new { type = "text", sessionId, stepNumber, text, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct);

    public Task RecordToolUseAsync(
        Guid runId,
        string sessionId,
        int stepNumber,
        string toolName,
        string inputJson,
        string outputJson,
        bool success,
        long durationMs,
        IReadOnlyList<RolloutMediaAttachment>? media = null,
        CancellationToken ct = default) =>
        AppendAsync(runId, new
        {
            type = "tool_use",
            sessionId,
            stepNumber,
            toolName,
            inputJson,
            outputJson,
            success,
            timing = new { durationMs },
            media,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, ct);

    public Task RecordStepFinishAsync(
        Guid runId,
        string sessionId,
        int stepNumber,
        string finishReason,
        RolloutUsage? usage = null,
        CancellationToken ct = default) =>
        AppendAsync(runId, new
        {
            type = "step_finish",
            sessionId,
            stepNumber,
            finishReason,
            usage,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, ct);

    public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) =>
        AppendAsync(runId, new { type = "error", sessionId, message, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct);

    public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) =>
        AppendAsync(runId, new { type = "permission", toolName, decision, reason, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct);

    public Task RecordSkillActivationAsync(
        Guid runId,
        string sessionId,
        string skillName,
        bool firstActivation,
        bool consentGranted,
        int contentChars,
        CancellationToken ct = default) =>
        AppendAsync(runId, new
        {
            type = "skill_activation",
            sessionId,
            skillName,
            firstActivation,
            consentGranted,
            contentChars,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, ct);

    public Task RecordCompactionAsync(
        Guid runId,
        string sessionId,
        int beforeChars,
        int afterChars,
        int beforeTurns,
        int afterTurns,
        string summaryJson,
        CancellationToken ct = default) =>
        AppendAsync(runId, new
        {
            type = "compaction",
            sessionId,
            beforeChars,
            afterChars,
            beforeTurns,
            afterTurns,
            summaryJson,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, ct);

    public Task RecordMemoryOperationAsync(
        Guid runId,
        string sessionId,
        string operation,
        string scope,
        string? key,
        string? kind,
        int resultCount,
        CancellationToken ct = default) =>
        AppendAsync(runId, new
        {
            type = "memory_operation",
            sessionId,
            operation,
            scope,
            key,
            kind,
            resultCount,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, ct);

    public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RolloutEntry>>(ReadJsonl(runId));

    public Task<IReadOnlyList<RolloutEntry>> ReplayAsync(Guid runId, CancellationToken ct = default) =>
        GetRolloutAsync(runId, ct);

    public async Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default)
    {
        await EnsureRolloutDbAsync(ct).ConfigureAwait(false);
        var hits = new List<RolloutSearchHit>();
        await using var conn = new SqliteConnection(_options.RolloutDbConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT run_id, step_number, tool_name, snippet(rollout_fts, 2, '[', ']', '…', 32)
            FROM rollout_fts WHERE rollout_fts MATCH $q LIMIT $lim
            """;
        cmd.Parameters.AddWithValue("$q", query);
        cmd.Parameters.AddWithValue("$lim", limit);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            hits.Add(new RolloutSearchHit(
                Guid.Parse(reader.GetString(0)),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                1.0));
        }

        return hits;
    }

    private async Task AppendAsync(Guid runId, object payload, CancellationToken ct)
    {
        var line = JsonSerializer.Serialize(payload);
        var path = GetRolloutPath(runId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var runLock = _runLocks.GetOrAdd(runId, static _ => new object());
        lock (runLock)
        {
            AppendLineWithRetry(path, line);
        }

        await IndexLineAsync(runId, line, ct).ConfigureAwait(false);
    }

    private void AppendLineWithRetry(string path, string line, int maxAttempts = 6)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (RustRolloutWriterBridge.TryAppendLine(path, line, _logger))
                    return;

                using var stream = new FileStream(
                    path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(line);
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts - 1)
            {
                _logger.LogDebug(ex, "[Rollout] append retry {Attempt}/{Max} for {Path}", attempt + 1, maxAttempts, path);
                Thread.Sleep(20 * (attempt + 1));
            }
        }
    }

    private async Task IndexLineAsync(Guid runId, string line, CancellationToken ct)
    {
        await EnsureRolloutDbAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown";
        var step = root.TryGetProperty("stepNumber", out var s) ? s.GetInt32() : 0;
        var tool = root.TryGetProperty("toolName", out var tn) ? tn.GetString() ?? string.Empty : string.Empty;
        var output = root.TryGetProperty("outputJson", out var o) ? o.GetString() ?? string.Empty : line;

        await using var conn = new SqliteConnection(_options.RolloutDbConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO rollout_index(run_id, step_number, tool_name, event_type, payload_json, output_text) VALUES ($r,$s,$t,$e,$p,$o)";
        cmd.Parameters.AddWithValue("$r", runId.ToString("D"));
        cmd.Parameters.AddWithValue("$s", step);
        cmd.Parameters.AddWithValue("$t", tool);
        cmd.Parameters.AddWithValue("$e", type);
        cmd.Parameters.AddWithValue("$p", line);
        cmd.Parameters.AddWithValue("$o", output);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        await using var fts = conn.CreateCommand();
        fts.CommandText = "INSERT INTO rollout_fts(run_id, step_number, tool_name, output_text) VALUES ($r,$s,$t,$o)";
        fts.Parameters.AddWithValue("$r", runId.ToString("D"));
        fts.Parameters.AddWithValue("$s", step);
        fts.Parameters.AddWithValue("$t", tool);
        fts.Parameters.AddWithValue("$o", output);
        await fts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task EnsureRolloutDbAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_options.RolloutDbPath)!);
        await using var conn = new SqliteConnection(_options.RolloutDbConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS rollout_index (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              run_id TEXT NOT NULL,
              step_number INTEGER NOT NULL,
              tool_name TEXT,
              event_type TEXT NOT NULL,
              payload_json TEXT NOT NULL,
              output_text TEXT
            );
            CREATE VIRTUAL TABLE IF NOT EXISTS rollout_fts USING fts5(run_id, step_number, tool_name, output_text);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private List<RolloutEntry> ReadJsonl(Guid runId)
    {
        var path = GetRolloutPath(runId);
        if (!File.Exists(path))
            return [];

        var runLock = _runLocks.GetOrAdd(runId, static _ => new object());
        lock (runLock)
        {
            var list = new List<RolloutEntry>();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown";
                var step = root.TryGetProperty("stepNumber", out var s) ? s.GetInt32() : 0;
                var sessionId = root.TryGetProperty("sessionId", out var sid) ? sid.GetString() : null;
                list.Add(new RolloutEntry(type, runId, sessionId, step, DateTime.UtcNow, line));
            }

            return list;
        }
    }

    private string GetRolloutPath(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "rollout.jsonl");
}
