using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.GitAutomation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class VerifyPassCheckpointService : IVerifyPassCheckpointService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IShadowGitCheckpointService? _gitCheckpoint;
    private readonly IShadowWorkspaceAccessor? _shadowAccessor;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<VerifyPassCheckpointService> _logger;

    public VerifyPassCheckpointService(
        IOptions<AgentRuntimeOptions> options,
        ILogger<VerifyPassCheckpointService> logger,
        IShadowGitCheckpointService? gitCheckpoint = null,
        IShadowWorkspaceAccessor? shadowAccessor = null)
    {
        _options = options.Value;
        _logger = logger;
        _gitCheckpoint = gitCheckpoint;
        _shadowAccessor = shadowAccessor;
    }

    public async Task RecordVerifyPassAsync(Guid runId, Guid? shadowWorkspaceId, CancellationToken ct = default)
    {
        if (_gitCheckpoint is null)
            return;

        if (shadowWorkspaceId is not Guid workspaceId
            || _shadowAccessor is null
            || !_shadowAccessor.TryGetWorkspace(workspaceId, out var workspace))
        {
            _logger.LogDebug(
                "[VerifyPassCheckpoint {RunId}] Skipped: shadow workspace unavailable",
                runId);
            return;
        }

        var attemptNumber = CountExistingCheckpoints(runId) + 1;
        var tag = IShadowGitCheckpointService.VerifyPassTagName(attemptNumber);
        var hostPath = workspace.HostPath;

        await _gitCheckpoint.TagVerifyPassAsync(hostPath, attemptNumber, ct).ConfigureAwait(false);
        var fileDiffs = await _gitCheckpoint.GetSnapshotDiffAtTagAsync(hostPath, tag, ct).ConfigureAwait(false);

        var snapshot = new RunDiffCheckpointSnapshot(
            runId,
            tag,
            attemptNumber,
            DateTime.UtcNow,
            fileDiffs.Select(MapFileDiff).ToList());

        await PersistSnapshotAsync(runId, snapshot, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[VerifyPassCheckpoint {RunId}] Recorded {Tag} with {FileCount} files",
            runId,
            tag,
            snapshot.Files.Count);
    }

    public Task<IReadOnlyList<RunDiffCheckpointSummary>> ListCheckpointsAsync(
        Guid runId,
        CancellationToken ct = default)
    {
        var dir = CheckpointsDir(runId);
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<RunDiffCheckpointSummary>>(Array.Empty<RunDiffCheckpointSummary>());

        var summaries = Directory.EnumerateFiles(dir, "snapshot.json", SearchOption.AllDirectories)
            .Select(path =>
            {
                try
                {
                    var snapshot = JsonSerializer.Deserialize<RunDiffCheckpointSnapshot>(File.ReadAllText(path));
                    return snapshot is null
                        ? null
                        : new RunDiffCheckpointSummary(
                            snapshot.Tag,
                            snapshot.AttemptNumber,
                            snapshot.TaggedAtUtc,
                            snapshot.Files.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Skipping malformed checkpoint snapshot at {Path}", path);
                    return null;
                }
            })
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderBy(s => s.AttemptNumber)
            .ToList();

        return Task.FromResult<IReadOnlyList<RunDiffCheckpointSummary>>(summaries);
    }

    public Task<RunDiffCheckpointSnapshot?> LoadSnapshotAsync(
        Guid runId,
        string tag,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return Task.FromResult<RunDiffCheckpointSnapshot?>(null);

        var path = SnapshotPath(runId, tag);
        if (!File.Exists(path))
            return Task.FromResult<RunDiffCheckpointSnapshot?>(null);

        try
        {
            var snapshot = JsonSerializer.Deserialize<RunDiffCheckpointSnapshot>(File.ReadAllText(path));
            return Task.FromResult(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load checkpoint snapshot {Tag} for run {RunId}", tag, runId);
            return Task.FromResult<RunDiffCheckpointSnapshot?>(null);
        }
    }

    private async Task PersistSnapshotAsync(
        Guid runId,
        RunDiffCheckpointSnapshot snapshot,
        CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(SnapshotPath(runId, snapshot.Tag));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(
            SnapshotPath(runId, snapshot.Tag),
            JsonSerializer.Serialize(snapshot, JsonOptions),
            ct).ConfigureAwait(false);
    }

    private int CountExistingCheckpoints(Guid runId)
    {
        var dir = CheckpointsDir(runId);
        return !Directory.Exists(dir)
            ? 0
            : Directory.EnumerateDirectories(dir).Count();
    }

    private string CheckpointsDir(Guid runId) =>
        Path.Combine(RunDir(runId), "checkpoints");

    private string SnapshotPath(Guid runId, string tag) =>
        Path.Combine(CheckpointsDir(runId), tag, "snapshot.json");

    private string RunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));

    private static RunDiffCheckpointFile MapFileDiff(ShadowGitFileDiff diff) =>
        new(
            diff.Path,
            MapChangeKind(diff.ChangeKind),
            InferLanguage(diff.Path),
            diff.UnifiedDiff);

    private static RunDiffChangeKind MapChangeKind(ShadowGitChangeKind kind) =>
        kind switch
        {
            ShadowGitChangeKind.Add => RunDiffChangeKind.Add,
            ShadowGitChangeKind.Delete => RunDiffChangeKind.Delete,
            _ => RunDiffChangeKind.Modify
        };

    private static string InferLanguage(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "cs" => "csharp",
            "ts" or "tsx" => "typescript",
            "js" or "jsx" => "javascript",
            "py" => "python",
            "json" => "json",
            "yaml" or "yml" => "yaml",
            "md" => "markdown",
            _ => ext.Length > 0 ? ext : "text"
        };
    }
}
