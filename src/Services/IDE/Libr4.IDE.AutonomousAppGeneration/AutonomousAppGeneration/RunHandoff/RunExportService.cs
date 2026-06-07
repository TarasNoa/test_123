using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunExportService : IRunExportService
{
    private const string SchemaVersion = "1.0.0";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IAppGenerationRepository _repository;
    private readonly RunSessionSnapshotExporter _sessionExporter;
    private readonly IAgentRunPermissionStore _permissions;
    private readonly IFlowProgressStore? _flowStore;
    private readonly ISpaceStore? _spaceStore;
    private readonly IRepairPlaybookStore? _playbookStore;
    private readonly IAgentSessionStore? _sessionStore;
    private readonly IShadowWorkspaceAccessor? _shadowAccessor;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly RunExportOptions _options;
    private readonly ILogger<RunExportService> _logger;
    private readonly string _exportRoot;

    public RunExportService(
        IAppGenerationRepository repository,
        RunSessionSnapshotExporter sessionExporter,
        IAgentRunPermissionStore permissions,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        IOptions<RunExportOptions> options,
        ILogger<RunExportService> logger,
        IFlowProgressStore? flowStore = null,
        ISpaceStore? spaceStore = null,
        IRepairPlaybookStore? playbookStore = null,
        IAgentSessionStore? sessionStore = null,
        IShadowWorkspaceAccessor? shadowAccessor = null)
    {
        _repository = repository;
        _sessionExporter = sessionExporter;
        _permissions = permissions;
        _runtimeOptions = runtimeOptions.Value;
        _options = options.Value;
        _logger = logger;
        _flowStore = flowStore;
        _spaceStore = spaceStore;
        _playbookStore = playbookStore;
        _sessionStore = sessionStore;
        _shadowAccessor = shadowAccessor;
        _exportRoot = string.IsNullOrWhiteSpace(_options.ExportRootPath)
            ? Path.Combine(Path.GetTempPath(), "libr4-run-exports")
            : _options.ExportRootPath;
        Directory.CreateDirectory(_exportRoot);
    }

    public async Task<RunExportResult?> ExportAsync(Guid runId, CancellationToken ct = default)
    {
        var orchestrator = await _repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator is null)
            return null;

        var generatedAt = DateTime.UtcNow;
        var exportId = $"run-{runId:D}-{generatedAt:yyyyMMddHHmmss}";
        var stagingDir = Path.Combine(Path.GetTempPath(), $"libr4-export-stage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDir);

        try
        {
            var handoff = await BuildHandoffSnapshotAsync(runId, orchestrator, ct).ConfigureAwait(false);
            var lastStep = await ResolveLastStepNumberAsync(runId, ct).ConfigureAwait(false);

            await _sessionExporter.ExportRunSessionsAsync(
                runId,
                Path.Combine(stagingDir, "agent_session.sqlite"),
                ct).ConfigureAwait(false);

            var workspacePath = await MaterializeWorkspaceAsync(orchestrator, stagingDir, ct).ConfigureAwait(false);
            if (workspacePath is not null)
            {
                await CreateTarGzAsync(
                    workspacePath,
                    Path.Combine(stagingDir, "workspace.tar.gz"),
                    _options.WorkspaceExcludeDirNames,
                    ct).ConfigureAwait(false);

                if (workspacePath.StartsWith(stagingDir, StringComparison.OrdinalIgnoreCase))
                    TryDeleteDirectory(workspacePath);
            }

            await CopyRunArtifactsAsync(runId, Path.Combine(stagingDir, "run-artifacts"), ct).ConfigureAwait(false);
            await WriteHandoffFilesAsync(stagingDir, handoff, ct).ConfigureAwait(false);

            var manifest = new RunExportManifest(
                SchemaVersion,
                runId,
                null,
                orchestrator.TenantId,
                orchestrator.RequestFingerprint,
                orchestrator.ShadowWorkspaceId,
                orchestrator.Files.Count,
                lastStep,
                orchestrator.Status.ToString(),
                orchestrator.FailureReason,
                generatedAt,
                string.Empty,
                0,
                handoff);

            var manifestPath = Path.Combine(stagingDir, "run-manifest.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, JsonOptions),
                ct).ConfigureAwait(false);

            var bundlePath = Path.Combine(_exportRoot, $"{exportId}.tar.gz");
            await CreateTarGzAsync(stagingDir, bundlePath, Array.Empty<string>(), ct).ConfigureAwait(false);

            var bundleBytes = new FileInfo(bundlePath).Length;
            if (bundleBytes > _options.MaxBundleBytes)
            {
                File.Delete(bundlePath);
                throw new InvalidOperationException(
                    $"Run export exceeds max bundle size ({bundleBytes} > {_options.MaxBundleBytes})");
            }

            var sha256 = await ComputeFileSha256Async(bundlePath, ct).ConfigureAwait(false);
            var addressedPath = Path.Combine(_exportRoot, $"{sha256}.tar.gz");
            if (!File.Exists(addressedPath))
                File.Move(bundlePath, addressedPath, overwrite: true);
            else
                File.Delete(bundlePath);

            var finalManifest = manifest with { BundleSha256 = sha256, BundleBytes = bundleBytes };
            await WriteSidecarManifestAsync(exportId, finalManifest, ct).ConfigureAwait(false);

            CleanupOldArtifacts();

            var expiresAt = generatedAt.AddDays(Math.Clamp(_options.RetentionDays, 1, 90));
            return new RunExportResult(
                runId,
                exportId,
                sha256,
                addressedPath,
                $"/api/v1/ide/app-generation/{runId:D}/export/{exportId}/download",
                bundleBytes,
                generatedAt,
                expiresAt);
        }
        finally
        {
            TryDeleteDirectory(stagingDir);
        }
    }

    public Task<(string Path, string FileName)?> TryResolveDownloadAsync(
        Guid runId,
        string exportId,
        CancellationToken ct = default)
    {
        var sidecar = Path.Combine(_exportRoot, $"{exportId}.manifest.json");
        if (!File.Exists(sidecar))
            return Task.FromResult<(string Path, string FileName)?>(null);

        try
        {
            var manifest = JsonSerializer.Deserialize<RunExportManifest>(File.ReadAllText(sidecar));
            if (manifest is null || manifest.RunId != runId)
                return Task.FromResult<(string Path, string FileName)?>(null);

            if (IsExpired(manifest))
            {
                _logger.LogInformation(
                    "Export {ExportId} for run {RunId} expired at {ExpiresAtUtc}",
                    exportId,
                    runId,
                    manifest.ExportedAtUtc.AddDays(Math.Clamp(_options.RetentionDays, 1, 90)));
                return Task.FromResult<(string Path, string FileName)?>(null);
            }

            var bundlePath = Path.Combine(_exportRoot, $"{manifest.BundleSha256}.tar.gz");
            if (!File.Exists(bundlePath))
                return Task.FromResult<(string Path, string FileName)?>(null);

            return Task.FromResult<(string Path, string FileName)?>(
                (bundlePath, $"run-export-{runId:D}-{manifest.BundleSha256[..12]}.tar.gz"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve export download for {RunId}/{ExportId}", runId, exportId);
            return Task.FromResult<(string Path, string FileName)?>(null);
        }
    }

    public int PruneExpiredExports()
    {
        var removed = 0;
        if (!Directory.Exists(_exportRoot))
            return removed;

        foreach (var sidecar in Directory.EnumerateFiles(_exportRoot, "*.manifest.json"))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<RunExportManifest>(File.ReadAllText(sidecar));
                if (manifest is null || !IsExpired(manifest))
                    continue;

                var bundlePath = Path.Combine(_exportRoot, $"{manifest.BundleSha256}.tar.gz");
                if (File.Exists(bundlePath))
                {
                    TryDeleteFile(bundlePath);
                    removed++;
                }

                TryDeleteFile(sidecar);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to prune export sidecar {Sidecar}", sidecar);
            }
        }

        CleanupOldArtifacts();
        return removed;
    }

    private bool IsExpired(RunExportManifest manifest)
    {
        var retention = Math.Clamp(_options.RetentionDays, 1, 90);
        return manifest.ExportedAtUtc.AddDays(retention) <= DateTime.UtcNow;
    }

    private async Task<RunExportHandoffSnapshot> BuildHandoffSnapshotAsync(
        Guid runId,
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct)
    {
        var mode = _permissions.Get(runId).ToString();
        var prompts = _permissions.GetAllPrompts(runId)
            .Select(p => new RunExportPermissionPrompt(
                p.Id,
                p.ToolName,
                p.Path,
                p.Reason,
                p.CreatedAtUtc,
                p.Accepted))
            .ToList();

        RunExportFlowSnapshot? flow = null;
        if (_flowStore is not null)
        {
            var progress = await _flowStore.LoadAsync(runId, ct).ConfigureAwait(false);
            if (progress is not null)
            {
                var stepIndex = progress.Nodes.ToList().FindIndex(n =>
                    string.Equals(n.NodeId, progress.CurrentNodeId, StringComparison.OrdinalIgnoreCase));
                flow = new RunExportFlowSnapshot(
                    progress.FlowName,
                    Math.Max(0, stepIndex),
                    progress.CurrentNodeId,
                    progress.UpdatedAtUtc);
            }
        }

        var hints = await ResolvePlaybookHintsAsync(runId, ct).ConfigureAwait(false);
        var spaces = await ResolveSpaceMembershipAsync(runId, ct).ConfigureAwait(false);

        return new RunExportHandoffSnapshot(mode, prompts, flow, hints, spaces);
    }

    private async Task<int> ResolveLastStepNumberAsync(Guid runId, CancellationToken ct)
    {
        if (_sessionStore is not null)
        {
            var session = await _sessionStore.GetLatestSessionByRunIdAsync(runId, ct).ConfigureAwait(false);
            if (session is not null)
                return session.CurrentStepNumber;
        }

        var rolloutPath = Path.Combine(RunDir(runId), "rollout.jsonl");
        if (!File.Exists(rolloutPath))
            return 0;

        var max = 0;
        foreach (var line in await File.ReadAllLinesAsync(rolloutPath, ct).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("stepNumber", out var step))
                    max = Math.Max(max, step.GetInt32());
            }
            catch (JsonException)
            {
                // ignore malformed lines
            }
        }

        return max;
    }

    private async Task<string?> MaterializeWorkspaceAsync(
        AppGenerationOrchestrator orchestrator,
        string stagingDir,
        CancellationToken ct)
    {
        if (orchestrator.ShadowWorkspaceId is Guid workspaceId
            && _shadowAccessor?.TryGetWorkspace(workspaceId, out var workspace) == true
            && Directory.Exists(workspace.HostPath))
        {
            return workspace.HostPath;
        }

        if (orchestrator.Files.Count == 0)
            return null;

        var workspacePath = Path.Combine(stagingDir, "workspace");
        Directory.CreateDirectory(workspacePath);
        foreach (var file in orchestrator.Files)
        {
            if (string.IsNullOrWhiteSpace(file.RelativePath))
                continue;

            var safe = file.RelativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            if (safe.Contains("..", StringComparison.Ordinal))
                continue;

            var abs = Path.Combine(workspacePath, safe);
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(abs, file.Content ?? string.Empty, ct).ConfigureAwait(false);
        }

        return workspacePath;
    }

    private async Task CopyRunArtifactsAsync(Guid runId, string destinationDir, CancellationToken ct)
    {
        var runDir = RunDir(runId);
        if (!Directory.Exists(runDir))
            return;

        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.EnumerateFiles(runDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(runDir, file);
            if (relative.StartsWith("exports", StringComparison.OrdinalIgnoreCase))
                continue;

            var target = Path.Combine(destinationDir, relative);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Copy(file, target, overwrite: true);
        }

        await Task.CompletedTask;
    }

    private static async Task WriteHandoffFilesAsync(
        string stagingDir,
        RunExportHandoffSnapshot handoff,
        CancellationToken ct)
    {
        var handoffDir = Path.Combine(stagingDir, "handoff");
        Directory.CreateDirectory(handoffDir);

        await File.WriteAllTextAsync(
            Path.Combine(handoffDir, "permissions.json"),
            JsonSerializer.Serialize(new { mode = handoff.PermissionMode, prompts = handoff.PermissionPrompts }, JsonOptions),
            ct).ConfigureAwait(false);

        if (handoff.Flow is not null)
        {
            await File.WriteAllTextAsync(
                Path.Combine(handoffDir, "flow.json"),
                JsonSerializer.Serialize(handoff.Flow, JsonOptions),
                ct).ConfigureAwait(false);
        }

        await File.WriteAllTextAsync(
            Path.Combine(handoffDir, "playbook-hints.json"),
            JsonSerializer.Serialize(handoff.PlaybookHints, JsonOptions),
            ct).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(handoffDir, "space-membership.json"),
            JsonSerializer.Serialize(handoff.SpaceMembership, JsonOptions),
            ct).ConfigureAwait(false);
    }

    private async Task WriteSidecarManifestAsync(string exportId, RunExportManifest manifest, CancellationToken ct)
    {
        var sidecarPath = Path.Combine(_exportRoot, $"{exportId}.manifest.json");
        await File.WriteAllTextAsync(sidecarPath, JsonSerializer.Serialize(manifest, JsonOptions), ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<RunExportPlaybookHint>> ResolvePlaybookHintsAsync(Guid runId, CancellationToken ct)
    {
        if (_playbookStore is null)
            return Array.Empty<RunExportPlaybookHint>();

        var signatures = ExtractPlaybookSignatures(runId);
        if (signatures.Count == 0)
            return Array.Empty<RunExportPlaybookHint>();

        var hints = new List<RunExportPlaybookHint>();
        foreach (var signature in signatures)
        {
            var hint = await _playbookStore.TryGetHintAsync(signature, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(hint))
                continue;

            hints.Add(new RunExportPlaybookHint(signature, hint, 0, DateTime.UtcNow));
        }

        return hints;
    }

    private HashSet<string> ExtractPlaybookSignatures(Guid runId)
    {
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rolloutPath = Path.Combine(RunDir(runId), "rollout.jsonl");
        if (!File.Exists(rolloutPath))
            return signatures;

        foreach (var line in File.ReadLines(rolloutPath))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("playbook", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("errorSignature", out var sigEl)
                    && sigEl.ValueKind == JsonValueKind.String)
                {
                    var sig = sigEl.GetString();
                    if (!string.IsNullOrWhiteSpace(sig))
                        signatures.Add(sig);
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        return signatures;
    }

    private async Task<IReadOnlyList<RunExportSpaceMembership>> ResolveSpaceMembershipAsync(
        Guid runId,
        CancellationToken ct)
    {
        if (_spaceStore is null)
            return Array.Empty<RunExportSpaceMembership>();

        var members = await _spaceStore.ListMembersByRunIdAsync(runId, ct).ConfigureAwait(false);
        var result = new List<RunExportSpaceMembership>(members.Count);
        foreach (var member in members)
        {
            var space = await _spaceStore.GetSpaceAsync(member.SpaceId, ct).ConfigureAwait(false);
            result.Add(new RunExportSpaceMembership(
                member.SpaceId,
                space?.Name ?? member.SpaceId.ToString("D"),
                member.MemberId,
                member.Role.ToString(),
                member.BranchName,
                member.Status.ToString()));
        }

        return result;
    }

    private string RunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_runtimeOptions.RunsRoot), runId.ToString("D"));

    private static async Task CreateTarGzAsync(
        string sourceDirectory,
        string outputPath,
        IReadOnlyList<string> excludeDirNames,
        CancellationToken ct)
    {
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var exclude = new HashSet<string>(excludeDirNames, StringComparer.OrdinalIgnoreCase);
        await using var fileStream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var gzip = new GZipStream(fileStream, CompressionLevel.Optimal);
        await using var tar = new TarWriter(gzip);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
            if (ShouldExclude(relative, exclude))
                continue;

            await using var input = File.OpenRead(file);
            var entry = new PaxTarEntry(TarEntryType.RegularFile, relative)
            {
                DataStream = input
            };
            tar.WriteEntry(entry);
        }
    }

    private static bool ShouldExclude(string relativePath, HashSet<string> excludeDirNames)
    {
        foreach (var segment in relativePath.Split('/', '\\'))
        {
            if (excludeDirNames.Contains(segment))
                return true;
        }

        return false;
    }

    private static async Task<string> ComputeFileSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void CleanupOldArtifacts()
    {
        if (!Directory.Exists(_exportRoot))
            return;

        var retention = Math.Clamp(_options.RetentionDays, 1, 90);
        var cutoff = DateTime.UtcNow.AddDays(-retention);
        var maxArtifacts = Math.Clamp(_options.MaxArtifacts, 10, 5000);

        foreach (var file in Directory.EnumerateFiles(_exportRoot, "*.tar.gz"))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTimeUtc < cutoff)
            {
                TryDeleteFile(file);
                continue;
            }
        }

        foreach (var sidecar in Directory.EnumerateFiles(_exportRoot, "*.manifest.json"))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<RunExportManifest>(File.ReadAllText(sidecar));
                if (manifest is not null && IsExpired(manifest))
                    TryDeleteFile(sidecar);
            }
            catch
            {
                // ignore malformed sidecars during cleanup
            }
        }

        var bundles = Directory.EnumerateFiles(_exportRoot, "*.tar.gz")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        foreach (var extra in bundles.Skip(maxArtifacts))
            TryDeleteFile(extra.FullName);
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }
}
