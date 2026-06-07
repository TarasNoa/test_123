using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunSyncOptions
{
    public const string SectionName = "AutonomousAppGeneration:RunSync";

    public bool Enabled { get; set; }

    /// <summary>Max file payload embedded in a delta (bytes). Larger files are skipped with hash-only.</summary>
    public int MaxInlineBytes { get; set; } = 256 * 1024;
}

public enum WorkspaceSyncDeltaKind
{
    Created,
    Modified,
    Deleted
}

public sealed record WorkspaceSyncDelta(
    Guid RunId,
    string RelativePath,
    WorkspaceSyncDeltaKind Kind,
    DateTime TimestampUtc,
    string Source,
    string? ContentBase64,
    string? ContentHash);

public enum RunSyncApplyStatus
{
    Applied,
    SkippedOlder,
    SkippedDuplicate,
    ConflictRecorded,
    Rejected
}

public sealed record RunSyncApplyResult(
    RunSyncApplyStatus Status,
    string? ConflictRelativePath = null,
    string? Message = null);

public sealed record RunSyncSession(
    Guid RunId,
    string WorkspaceRoot,
    string Role,
    DateTime StartedAtUtc);

public sealed record RunSyncConflictRecord(
    string RelativePath,
    string WinnerSource,
    string LoserSource,
    DateTime TimestampUtc,
    string? ConflictFile);

public interface IRunSyncCoordinator
{
    RunSyncSession RegisterSession(Guid runId, string workspaceRoot, string role);

    void UnregisterSession(Guid runId);

    bool TryGetSession(Guid runId, out RunSyncSession session);

    Task<RunSyncApplyResult> ApplyDeltaAsync(WorkspaceSyncDelta delta, CancellationToken ct = default);

    WorkspaceSyncDelta? CreateDeltaFromFileChange(
        Guid runId,
        string role,
        WorkspaceFileChange change);

    Task<IReadOnlyList<RunSyncConflictRecord>> GetPendingConflictsAsync(Guid runId, CancellationToken ct = default);
}

public sealed class RunSyncCoordinator : IRunSyncCoordinator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RunSyncOptions _options;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly ILogger<RunSyncCoordinator> _logger;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, RunSyncSession> _sessions = new();
    private readonly Dictionary<Guid, Dictionary<string, FileSyncState>> _fileStates = new();

    public RunSyncCoordinator(
        IOptions<RunSyncOptions> options,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        ILogger<RunSyncCoordinator> logger)
    {
        _options = options.Value;
        _runtimeOptions = runtimeOptions.Value;
        _logger = logger;
    }

    public RunSyncSession RegisterSession(Guid runId, string workspaceRoot, string role)
    {
        var normalizedRoot = Path.GetFullPath(workspaceRoot);
        var session = new RunSyncSession(runId, normalizedRoot, role.Trim().ToLowerInvariant(), DateTime.UtcNow);
        lock (_sync)
        {
            _sessions[runId] = session;
            _fileStates.TryAdd(runId, new Dictionary<string, FileSyncState>(StringComparer.OrdinalIgnoreCase));
        }

        _logger.LogInformation("Run sync session registered run={RunId} role={Role} root={Root}", runId, role, normalizedRoot);
        return session;
    }

    public void UnregisterSession(Guid runId)
    {
        lock (_sync)
        {
            _sessions.Remove(runId);
            _fileStates.Remove(runId);
        }
    }

    public bool TryGetSession(Guid runId, out RunSyncSession session)
    {
        lock (_sync)
        {
            return _sessions.TryGetValue(runId, out session!);
        }
    }

    public async Task<RunSyncApplyResult> ApplyDeltaAsync(WorkspaceSyncDelta delta, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new RunSyncApplyResult(RunSyncApplyStatus.Rejected, Message: "run_sync_disabled");

        if (!TryGetSession(delta.RunId, out var session))
            return new RunSyncApplyResult(RunSyncApplyStatus.Rejected, Message: "run_sync_session_not_found");

        var relativePath = NormalizeRelativePath(delta.RelativePath);
        if (string.IsNullOrWhiteSpace(relativePath))
            return new RunSyncApplyResult(RunSyncApplyStatus.Rejected, Message: "invalid_relative_path");

        var targetPath = Path.GetFullPath(Path.Combine(session.WorkspaceRoot, relativePath));
        if (!targetPath.StartsWith(session.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
            return new RunSyncApplyResult(RunSyncApplyStatus.Rejected, Message: "path_escape_denied");

        FileSyncState? previous;
        lock (_sync)
        {
            if (!_fileStates.TryGetValue(delta.RunId, out var states))
            {
                states = new Dictionary<string, FileSyncState>(StringComparer.OrdinalIgnoreCase);
                _fileStates[delta.RunId] = states;
            }
            states.TryGetValue(relativePath, out previous);
        }

        if (previous is not null)
        {
            if (delta.TimestampUtc < previous.TimestampUtc)
                return new RunSyncApplyResult(RunSyncApplyStatus.SkippedOlder);

            if (delta.TimestampUtc == previous.TimestampUtc
                && string.Equals(previous.ContentHash, delta.ContentHash, StringComparison.OrdinalIgnoreCase))
                return new RunSyncApplyResult(RunSyncApplyStatus.SkippedDuplicate);

            if (delta.TimestampUtc == previous.TimestampUtc
                && !string.Equals(previous.ContentHash, delta.ContentHash, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previous.Source)
                && !string.Equals(previous.Source, delta.Source, StringComparison.OrdinalIgnoreCase))
            {
                var conflictPath = await RecordConflictAsync(session, relativePath, delta, previous, ct).ConfigureAwait(false);
                return new RunSyncApplyResult(RunSyncApplyStatus.ConflictRecorded, conflictPath);
            }
        }

        if (delta.Kind == WorkspaceSyncDeltaKind.Deleted)
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
        else
        {
            var content = DecodeContent(delta);
            if (content is null)
                return new RunSyncApplyResult(RunSyncApplyStatus.Rejected, Message: "missing_inline_content");

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await File.WriteAllBytesAsync(targetPath, content, ct).ConfigureAwait(false);
        }

        lock (_sync)
        {
            if (!_fileStates.TryGetValue(delta.RunId, out var states))
            {
                states = new Dictionary<string, FileSyncState>(StringComparer.OrdinalIgnoreCase);
                _fileStates[delta.RunId] = states;
            }

            states[relativePath] = new FileSyncState(delta.TimestampUtc, delta.Source, delta.ContentHash ?? string.Empty);
        }

        return new RunSyncApplyResult(RunSyncApplyStatus.Applied);
    }

    public WorkspaceSyncDelta? CreateDeltaFromFileChange(Guid runId, string role, WorkspaceFileChange change)
    {
        if (!_options.Enabled || !TryGetSession(runId, out var session))
            return null;

        var relativePath = NormalizeRelativePath(change.RelativePath);
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var kind = change.Kind switch
        {
            WorkspaceFileChangeKind.Deleted => WorkspaceSyncDeltaKind.Deleted,
            WorkspaceFileChangeKind.Created => WorkspaceSyncDeltaKind.Created,
            _ => WorkspaceSyncDeltaKind.Modified
        };

        string? contentBase64 = null;
        string? contentHash = null;
        if (kind != WorkspaceSyncDeltaKind.Deleted)
        {
            var fullPath = Path.Combine(session.WorkspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
                return null;

            var bytes = File.ReadAllBytes(fullPath);
            contentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (bytes.Length <= _options.MaxInlineBytes)
                contentBase64 = Convert.ToBase64String(bytes);
        }

        return new WorkspaceSyncDelta(
            runId,
            relativePath,
            kind,
            change.TimestampUtc,
            role.Trim().ToLowerInvariant(),
            contentBase64,
            contentHash);
    }

    public Task<IReadOnlyList<RunSyncConflictRecord>> GetPendingConflictsAsync(Guid runId, CancellationToken ct = default)
    {
        var path = Path.Combine(GetRunDir(runId), "handoff", "sync-conflicts.jsonl");
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<RunSyncConflictRecord>>(Array.Empty<RunSyncConflictRecord>());

        var list = new List<RunSyncConflictRecord>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeEl)
                && string.Equals(typeEl.GetString(), "conflict", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new RunSyncConflictRecord(
                    root.TryGetProperty("path", out var pathEl) ? pathEl.GetString() ?? "" : "",
                    root.TryGetProperty("winnerSource", out var winnerEl) ? winnerEl.GetString() ?? "" : "",
                    root.TryGetProperty("loserSource", out var loserEl) ? loserEl.GetString() ?? "" : "",
                    root.TryGetProperty("timestampUtc", out var tsEl) && tsEl.TryGetDateTime(out var ts)
                        ? ts
                        : DateTime.UtcNow,
                    root.TryGetProperty("conflictFile", out var cfEl) ? cfEl.GetString() : null));
                continue;
            }

            var delta = JsonSerializer.Deserialize<WorkspaceSyncDelta>(line, JsonOptions);
            if (delta is not null)
            {
                list.Add(new RunSyncConflictRecord(
                    delta.RelativePath,
                    delta.Source,
                    "?",
                    delta.TimestampUtc,
                    null));
            }
        }

        return Task.FromResult<IReadOnlyList<RunSyncConflictRecord>>(list);
    }

    private async Task<string> RecordConflictAsync(
        RunSyncSession session,
        string relativePath,
        WorkspaceSyncDelta incoming,
        FileSyncState previous,
        CancellationToken ct)
    {
        var conflictDir = Path.Combine(GetRunDir(session.RunId), "handoff", "sync-conflicts");
        Directory.CreateDirectory(conflictDir);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var conflictName = $"{relativePath.Replace('/', '_')}.{incoming.Source}.{stamp}.bak";
        var conflictPath = Path.Combine(conflictDir, conflictName);

        var incomingContent = DecodeContent(incoming);
        if (incomingContent is not null)
            await File.WriteAllBytesAsync(conflictPath, incomingContent, ct).ConfigureAwait(false);

        var auditPath = Path.Combine(GetRunDir(session.RunId), "handoff", "sync-conflicts.jsonl");
        var marker = new
        {
            type = "conflict",
            runId = session.RunId,
            path = relativePath,
            winnerSource = previous.Source,
            loserSource = incoming.Source,
            timestampUtc = DateTime.UtcNow,
            conflictFile = Path.GetRelativePath(GetRunDir(session.RunId), conflictPath).Replace('\\', '/')
        };
        await File.AppendAllTextAsync(auditPath, JsonSerializer.Serialize(marker, JsonOptions) + Environment.NewLine, ct)
            .ConfigureAwait(false);

        _logger.LogWarning(
            "Run sync conflict run={RunId} path={Path} winner={Winner} loser={Loser}",
            session.RunId,
            relativePath,
            previous.Source,
            incoming.Source);

        return marker.conflictFile;
    }

    private string GetRunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_runtimeOptions.RunsRoot), runId.ToString("D"));

    private static byte[]? DecodeContent(WorkspaceSyncDelta delta)
    {
        if (string.IsNullOrWhiteSpace(delta.ContentBase64))
            return null;

        try
        {
            return Convert.FromBase64String(delta.ContentBase64);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('/');

    private sealed record FileSyncState(DateTime TimestampUtc, string Source, string ContentHash);
}
