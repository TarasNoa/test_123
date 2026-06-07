using System.Security.Cryptography;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunImportService : IRunImportService
{
    private readonly IAppGenerationRepository _repository;
    private readonly IRunImportIdempotencyStore _idempotency;
    private readonly RunSessionSnapshotImporter _sessionImporter;
    private readonly RunEnvironmentUrlRemapper _urlRemapper;
    private readonly IAgentRunPermissionStore _permissions;
    private readonly IShadowExecutionService? _shadow;
    private readonly IFlowProgressStore? _flowStore;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly RunImportOptions _options;
    private readonly ILogger<RunImportService> _logger;

    public RunImportService(
        IAppGenerationRepository repository,
        IRunImportIdempotencyStore idempotency,
        RunSessionSnapshotImporter sessionImporter,
        RunEnvironmentUrlRemapper urlRemapper,
        IAgentRunPermissionStore permissions,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        IOptions<RunImportOptions> options,
        ILogger<RunImportService> logger,
        IShadowExecutionService? shadow = null,
        IFlowProgressStore? flowStore = null)
    {
        _repository = repository;
        _idempotency = idempotency;
        _sessionImporter = sessionImporter;
        _urlRemapper = urlRemapper;
        _permissions = permissions;
        _runtimeOptions = runtimeOptions.Value;
        _options = options.Value;
        _logger = logger;
        _shadow = shadow;
        _flowStore = flowStore;
    }

    public Task<RunImportResult> ImportBundleAsync(string bundlePath, CancellationToken ct = default) =>
        ImportInternalAsync(bundlePath, null, ct);

    public async Task<RunImportResult> ImportBundleStreamAsync(
        Stream bundleStream,
        string suggestedFileName,
        CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"libr4-import-{Guid.NewGuid():N}.tar.gz");
        await using (var output = File.Create(tempPath))
            await bundleStream.CopyToAsync(output, ct).ConfigureAwait(false);
        return await ImportInternalAsync(tempPath, suggestedFileName, ct).ConfigureAwait(false);
    }

    private async Task<RunImportResult> ImportInternalAsync(
        string bundlePath,
        string? suggestedFileName,
        CancellationToken ct)
    {
        if (!File.Exists(bundlePath))
            throw new RunImportException("bundle_not_found", "Import bundle file was not found");

        var bundleBytes = new FileInfo(bundlePath).Length;
        if (bundleBytes > _options.MaxBundleBytes)
            throw new RunImportException("bundle_too_large", $"Import bundle exceeds max size ({bundleBytes} bytes)");

        var bundleSha = await ComputeFileSha256Async(bundlePath, ct).ConfigureAwait(false);
        var existing = await _idempotency.FindByBundleShaAsync(bundleSha, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return new RunImportResult(
                existing.ImportedRunId,
                existing.SourceRunId,
                bundleSha,
                existing.LastStepNumber,
                IdempotentReplay: true,
                existing.ImportedAtUtc,
                ResumeHint: $"resume_step_{existing.LastStepNumber + 1}");
        }

        var extractRoot = Path.Combine(Path.GetTempPath(), $"libr4-import-extract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractRoot);

        try
        {
            await RunBundleArchive.ExtractTarGzAsync(bundlePath, extractRoot, ct).ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            throw new RunImportException("bundle_corrupt", $"Import bundle is corrupted or unreadable: {ex.Message}");
        }
        catch (IOException ex)
        {
            throw new RunImportException("bundle_corrupt", $"Import bundle is corrupted or unreadable: {ex.Message}");
        }

        try
        {
            var manifest = await RunBundleArchive.ReadManifestAsync(extractRoot, ct).ConfigureAwait(false);
            if (manifest is null)
                throw new RunImportException("manifest_missing", "run-manifest.json is missing from import bundle");

            if (!string.IsNullOrWhiteSpace(manifest.BundleSha256)
                && !string.Equals(manifest.BundleSha256, bundleSha, StringComparison.OrdinalIgnoreCase))
            {
                throw new RunImportException(
                    "manifest_sha_mismatch",
                    "Bundle SHA-256 does not match run-manifest.json");
            }

            var importedAt = DateTime.UtcNow;
            var fingerprint = AppGenerationRequestFingerprint.Build(
                $"import:{manifest.RunId:D}:{bundleSha}",
                maxIterations: 20,
                triggerSource: "run_import",
                triggerActor: "system",
                tenantId: manifest.TenantId);

            var orchestrator = AppGenerationOrchestrator.Create(
                $"Imported run from {manifest.RunId:D}",
                fingerprint);

            if (!string.IsNullOrWhiteSpace(manifest.TenantId))
                orchestrator.SetTenantId(manifest.TenantId);

            var plan = BuildImportPlan(manifest);
            orchestrator.AttachPlan(plan);

            var files = await LoadWorkspaceFilesAsync(extractRoot, ct).ConfigureAwait(false);
            foreach (var file in files)
                orchestrator.UpsertFile(file);

            if (_shadow is not null && files.Count > 0)
            {
                var workspaceId = await _shadow.PrepareWorkspaceAsync(files, plan.RuntimeImage, ct)
                    .ConfigureAwait(false);
                orchestrator.AttachShadowWorkspace(workspaceId);
            }

            orchestrator.BeginGeneration();
            await _repository.SaveAsync(orchestrator, ct).ConfigureAwait(false);

            var newRunId = orchestrator.Id;
            var runDir = RunDir(newRunId);
            Directory.CreateDirectory(runDir);

            var artifactsSource = Path.Combine(extractRoot, "run-artifacts");
            if (Directory.Exists(artifactsSource))
                CopyDirectoryContents(artifactsSource, runDir, overwrite: true);

            await _urlRemapper.RemapRunArtifactsAsync(newRunId, runDir, ct).ConfigureAwait(false);

            var snapshotPath = Path.Combine(extractRoot, "agent_session.sqlite");
            await _sessionImporter.ImportAsync(newRunId, snapshotPath, ct).ConfigureAwait(false);

            await RestoreHandoffAsync(newRunId, extractRoot, manifest, ct).ConfigureAwait(false);

            var lineage = new RunImportLineage(
                manifest.RunId,
                newRunId,
                bundleSha,
                manifest.LastStepNumber,
                importedAt);

            await WriteLineageAsync(newRunId, lineage, ct).ConfigureAwait(false);
            await _idempotency.SaveAsync(lineage, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Imported run {SourceRunId} as {NewRunId} (sha={Sha}, step={Step})",
                manifest.RunId,
                newRunId,
                bundleSha,
                manifest.LastStepNumber);

            return new RunImportResult(
                newRunId,
                manifest.RunId,
                bundleSha,
                manifest.LastStepNumber,
                IdempotentReplay: false,
                importedAt,
                ResumeHint: $"resume_step_{manifest.LastStepNumber + 1}");
        }
        finally
        {
            TryDeleteDirectory(extractRoot);
            if (suggestedFileName is not null)
                TryDeleteFile(bundlePath);
        }
    }

    private async Task RestoreHandoffAsync(
        Guid newRunId,
        string extractRoot,
        RunExportManifest manifest,
        CancellationToken ct)
    {
        var handoffDir = Path.Combine(extractRoot, "handoff");
        if (Directory.Exists(handoffDir))
        {
            var targetHandoff = Path.Combine(RunDir(newRunId), "handoff");
            Directory.CreateDirectory(targetHandoff);
            CopyDirectory(handoffDir, targetHandoff, overwrite: true);
        }

        if (Enum.TryParse<AgentPermissionMode>(manifest.Handoff.PermissionMode, true, out var mode))
            _permissions.Set(newRunId, mode);

        foreach (var prompt in manifest.Handoff.PermissionPrompts)
        {
            _permissions.EnqueuePrompt(
                newRunId,
                new AgentPermissionPrompt(
                    prompt.Id,
                    prompt.ToolName,
                    prompt.Path,
                    prompt.Reason,
                    prompt.CreatedAtUtc,
                    prompt.Accepted));
        }

        if (_flowStore is not null && manifest.Handoff.Flow is { } flowSnapshot)
        {
            await _flowStore.SaveAsync(
                new FlowProgress(
                    newRunId,
                    flowSnapshot.FlowId,
                    flowSnapshot.CurrentStepId,
                    "imported",
                    Array.Empty<FlowNodeProgress>(),
                    flowSnapshot.UpdatedAtUtc),
                ct).ConfigureAwait(false);
        }

        var resumePayload = new
        {
            runId = newRunId,
            sourceRunId = manifest.RunId,
            lastStepNumber = manifest.LastStepNumber,
            resumeAtStep = manifest.LastStepNumber + 1,
            importedAtUtc = DateTime.UtcNow
        };

        var resumeDir = Path.Combine(RunDir(newRunId), "handoff");
        Directory.CreateDirectory(resumeDir);
        await File.WriteAllTextAsync(
            Path.Combine(resumeDir, "resume.json"),
            JsonSerializer.Serialize(resumePayload, RunBundleArchive.JsonOptions()),
            ct).ConfigureAwait(false);
    }

    private async Task WriteLineageAsync(Guid newRunId, RunImportLineage lineage, CancellationToken ct)
    {
        var path = Path.Combine(RunDir(newRunId), "handoff", "lineage.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(lineage, RunBundleArchive.JsonOptions()), ct)
            .ConfigureAwait(false);
    }

    private static GenerationPlan BuildImportPlan(RunExportManifest manifest) =>
        new(
            applicationName: $"imported-{manifest.RunId:N}"[..Math.Min(32, $"imported-{manifest.RunId:N}".Length)],
            applicationDescription: $"Imported from run {manifest.RunId:D}",
            techStack: new TechStack(
                new[] { "unknown" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                "imported"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 20);

    private async Task<List<GeneratedFile>> LoadWorkspaceFilesAsync(string extractRoot, CancellationToken ct)
    {
        var workspaceArchive = Path.Combine(extractRoot, "workspace.tar.gz");
        if (!File.Exists(workspaceArchive))
            return new List<GeneratedFile>();

        var workspaceDir = Path.Combine(extractRoot, "workspace-extracted");
        await RunBundleArchive.ExtractTarGzAsync(workspaceArchive, workspaceDir, ct).ConfigureAwait(false);

        var files = new List<GeneratedFile>();
        foreach (var path in Directory.EnumerateFiles(workspaceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(workspaceDir, path).Replace('\\', '/');
            if (relative.Split('/').Any(p => p is "node_modules" or ".venv" or ".git"))
                continue;

            var content = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            files.Add(new GeneratedFile(relative, InferLanguage(relative), content));
        }

        return files;
    }

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

    private string RunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_runtimeOptions.RunsRoot), runId.ToString("D"));

    private static async Task<string> ComputeFileSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void CopyDirectoryContents(string source, string destination, bool overwrite)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);
            File.Copy(file, target, overwrite);
        }
    }

    private static void CopyDirectory(string source, string destination, bool overwrite)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination, StringComparison.OrdinalIgnoreCase));

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(source, destination, StringComparison.OrdinalIgnoreCase);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);
            File.Copy(file, target, overwrite);
        }
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

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}
