using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Services;

public sealed class ShadowAgentRepairService : IShadowAgentRepairService
{
    private readonly IAgentSession _session;
    private readonly IShadowWorkspaceAccessor _workspace;
    private readonly IShadowExecutionService _shadow;
    private readonly RepairPlaybookService _playbook;
    private readonly IPlatformJitCapabilityService? _platformJit;
    private readonly IAgentSessionStore? _sessionStore;
    private readonly IAgentRunPermissionStore? _permissionStore;
    private readonly IFimPromptBuilder? _fimBuilder;
    private readonly IContextFragmentRepairAssembler? _fragmentAssembler;
    private readonly IDesignArtifactService? _designArtifacts;
    private readonly IVerifyFailureContextStore? _verifyFailures;
    private readonly IVerifyEvidenceStore? _verifyEvidence;
    private readonly ILspBridge? _lspBridge;
    private readonly IShadowGitCheckpointService? _gitCheckpoint;
    private readonly IFastContextPrefetcher? _fastContext;
    private readonly ShadowGitCheckpointOptions _gitOptions;
    private readonly AutonomousLoopGuardOptions _loopGuard;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<ShadowAgentRepairService> _logger;

    public ShadowAgentRepairService(
        IAgentSession session,
        IShadowWorkspaceAccessor workspace,
        IShadowExecutionService shadow,
        RepairPlaybookService playbook,
        IOptions<AgentRuntimeOptions> options,
        ILogger<ShadowAgentRepairService> logger,
        IPlatformJitCapabilityService? platformJit = null,
        IAgentSessionStore? sessionStore = null,
        IAgentRunPermissionStore? permissionStore = null,
        IFimPromptBuilder? fimBuilder = null,
        IOptions<AutonomousLoopGuardOptions>? loopGuard = null,
        IContextFragmentRepairAssembler? fragmentAssembler = null,
        IDesignArtifactService? designArtifacts = null,
        IVerifyFailureContextStore? verifyFailures = null,
        IVerifyEvidenceStore? verifyEvidence = null,
        ILspBridge? lspBridge = null,
        IShadowGitCheckpointService? gitCheckpoint = null,
        IOptions<ShadowGitCheckpointOptions>? gitOptions = null,
        IFastContextPrefetcher? fastContext = null)
    {
        _session = session;
        _workspace = workspace;
        _shadow = shadow;
        _playbook = playbook;
        _platformJit = platformJit;
        _sessionStore = sessionStore;
        _permissionStore = permissionStore;
        _fimBuilder = fimBuilder;
        _fragmentAssembler = fragmentAssembler;
        _designArtifacts = designArtifacts;
        _verifyFailures = verifyFailures;
        _verifyEvidence = verifyEvidence;
        _lspBridge = lspBridge;
        _gitCheckpoint = gitCheckpoint;
        _fastContext = fastContext;
        _gitOptions = gitOptions?.Value ?? new ShadowGitCheckpointOptions();
        _loopGuard = loopGuard?.Value ?? new AutonomousLoopGuardOptions();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeneratedFile>> RunRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        Guid workspaceId,
        string buildLog,
        IReadOnlyList<ErrorReport> errors,
        Guid? runId = null,
        int repairAttempt = 1,
        string? tenantUserId = null,
        CancellationToken ct = default)
    {
        if (!_options.UseAgentRuntimeRepair)
            return Array.Empty<GeneratedFile>();

        if (!_workspace.TryGetWorkspace(workspaceId, out var wsContext))
        {
            _logger.LogWarning("Agent repair skipped: workspace {Ws} not found", workspaceId);
            return Array.Empty<GeneratedFile>();
        }

        var working = currentFiles.Select(f => new GeneratedFile(f.RelativePath, f.Language, f.Content)).ToList();
        var playbookHint = _options.EnableRepairPlaybook
            ? await _playbook.TryGetHintAsync(errors, buildLog, plan, ct).ConfigureAwait(false)
            : null;
        var hadPlaybookHint = !string.IsNullOrWhiteSpace(playbookHint);

        string? orchestratorJitHint = null;
        if (_platformJit is not null && runId is Guid jitRunId)
        {
            var jit = await _platformJit.TryInjectForRepairAsync(
                jitRunId,
                repairAttempt,
                repairAttempt,
                errors,
                buildLog,
                plan,
                ct).ConfigureAwait(false);
            if (jit.Injected)
                orchestratorJitHint = jit.InjectionText;
        }

        if (hadPlaybookHint && runId is Guid hintRunId)
            RunPlaybookStats.RecordAttempt(Path.Combine(Path.GetFullPath(_options.RunsRoot), hintRunId.ToString("D")));
        var designArtifactJson = await TryLoadDesignArtifactJsonAsync(runId, ct).ConfigureAwait(false);
        var verifyEvidence = ResolveVerifyEvidence(runId);
        var focusPaths = errors
            .Select(e => e.FilePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        string? lspDiagnostics = null;
        if (_lspBridge is not null)
        {
            var lsp = await _lspBridge.GetWorkspaceContextAsync(
                    working,
                    plan,
                    errors,
                    focusPaths,
                    ct)
                .ConfigureAwait(false);
            lspDiagnostics = lsp.FormatForContextPack(3500);
            if (string.IsNullOrWhiteSpace(lspDiagnostics))
                lspDiagnostics = null;
        }

        string? gitDiffEvidence = null;
        if (_gitCheckpoint is not null && !string.IsNullOrWhiteSpace(wsContext.HostPath))
        {
            gitDiffEvidence = await _gitCheckpoint.GetWorkingDiffAsync(
                    wsContext.HostPath,
                    _gitOptions.MaxDiffChars,
                    ct)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(gitDiffEvidence))
                gitDiffEvidence = null;

            await _gitCheckpoint.TagRepairAttemptAsync(wsContext.HostPath, repairAttempt, ct).ConfigureAwait(false);
        }

        string? fastContextEvidence = null;
        if (_fastContext is not null)
        {
            var prefetch = await _fastContext.PrefetchForRepairAsync(
                    new FastContextPrefetchRequest(
                        wsContext.HostPath,
                        buildLog,
                        errors,
                        working,
                        RunId: runId),
                    ct)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(prefetch.FormattedText))
                fastContextEvidence = prefetch.FormattedText;
        }

        var contextFragments = _fragmentAssembler?.Assemble(new RepairFragmentInput(
            buildLog,
            errors,
            working,
            repairAttempt,
            designArtifactJson,
            VerifyEvidence: verifyEvidence,
            PlaybookHint: playbookHint,
            OrchestratorJitHint: orchestratorJitHint,
            LspDiagnostics: lspDiagnostics,
            GitDiffEvidence: gitDiffEvidence,
            FastContextEvidence: fastContextEvidence));
        var objective = BuildObjective(playbookHint);

        _logger.LogInformation(
            "Agent runtime repair starting for {App} in workspace {Ws} (errors={Count}, runId={RunId})",
            plan.ApplicationName,
            workspaceId,
            errors.Count,
            runId);

        var lastErrors = errors
            .Take(24)
            .Select(e => string.IsNullOrWhiteSpace(e.FilePath) ? e.Message : $"{e.FilePath}: {e.Message}")
            .ToArray();

        var fimContext = TryBuildFimContext(working, errors);
        var request = new AgentSessionRunRequest(
            objective,
            wsContext,
            working,
            plan,
            _workspace,
            AgentSessionMode.Repair,
            buildLog,
            RunId: runId,
            TenantUserId: tenantUserId,
            PromptStage: BuiltinPromptStage.Repairing,
            RepairAttempt: repairAttempt,
            LastErrors: lastErrors,
            ManifestFiles: currentFiles.Select(f => f.RelativePath).Take(48).ToArray(),
            Fim: fimContext,
            ContextFragments: contextFragments);

        AgentSessionResult result;
        if (_options.EnableSessionPersistence && _sessionStore is not null && runId is Guid id)
        {
            var existing = await _sessionStore.GetLatestSessionByRunIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null && !string.Equals(existing.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Resuming agent session {SessionId} for run {RunId}", existing.SessionId, id);
                result = await _session.ResumeAsync(existing.SessionId, request, ct).ConfigureAwait(false);
            }
            else
            {
                result = await _session.RunAsync(request, ct).ConfigureAwait(false);
            }
        }
        else
        {
            result = await _session.RunAsync(request, ct).ConfigureAwait(false);
        }

        foreach (var line in result.Trace)
            _logger.LogDebug("Agent trace: {Line}", line);

        if (result.Patches.Count == 0)
        {
            if (_options.EnableRepairPlaybook && errors.Count > 0)
            {
                await _playbook.RecordOutcomeAsync(
                    errors,
                    buildLog,
                    plan,
                    "repair_session:no_patches",
                    succeeded: false,
                    ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Agent runtime repair produced no patches (turns={Turns}, summary={Summary})",
                result.TurnsUsed,
                result.Summary);
            return Array.Empty<GeneratedFile>();
        }

        if (_options.EnableRepairPlaybook && errors.Count > 0)
        {
            await _playbook.RecordOutcomeAsync(
                errors,
                buildLog,
                plan,
                $"repair_session:{result.Patches.Count}_patches",
                succeeded: true,
                ct).ConfigureAwait(false);
        }

        if (hadPlaybookHint && runId is Guid hitRunId)
            RunPlaybookStats.RecordHit(Path.Combine(Path.GetFullPath(_options.RunsRoot), hitRunId.ToString("D")));

        await _shadow.UpdateWorkspaceAsync(workspaceId, result.Patches, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Agent runtime repair applied {Count} patch(es) in {Turns} turn(s): {Summary}",
            result.Patches.Count,
            result.TurnsUsed,
            result.Summary);

        return result.Patches;
    }

    private FimGenerationContext? TryBuildFimContext(
        IReadOnlyList<GeneratedFile> working,
        IReadOnlyList<ErrorReport> errors)
    {
        if (!_loopGuard.UseFimRepair || _fimBuilder is null)
            return null;

        var root = errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.FilePath));
        if (root is null)
            return null;

        var target = working.FirstOrDefault(f =>
            f.RelativePath.Equals(root.FilePath, StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Replace('\\', '/').EndsWith(
                root.FilePath!.Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase));
        if (target is null || !_fimBuilder.ShouldUseFim(target, root, _loopGuard.FimMinFileLines))
            return null;

        return _fimBuilder.TryBuild(
            target.RelativePath,
            target.Content ?? string.Empty,
            root.LineNumber,
            _loopGuard.FimHoleRadiusLines,
            out var prompt)
            ? _fimBuilder.ToGenerationContext(prompt)
            : null;
    }

    private async Task<string?> TryLoadDesignArtifactJsonAsync(Guid? runId, CancellationToken ct)
    {
        if (_designArtifacts is null || runId is not Guid id)
            return null;

        var artifact = await _designArtifacts.GetArtifactByRunAsync(id.ToString("D"), ct).ConfigureAwait(false);
        return artifact is null ? null : _designArtifacts.SerializeArtifact(artifact);
    }

    private string? ResolveVerifyEvidence(Guid? runId)
    {
        if (_verifyFailures is null || runId is not Guid id || !_verifyFailures.TryGet(id, out var evidence) || evidence is null)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine(evidence.Summary);
        if (!string.IsNullOrWhiteSpace(evidence.ReportText))
            sb.AppendLine(evidence.ReportText);
        if (!string.IsNullOrWhiteSpace(evidence.ReadinessEvidencePath))
            sb.AppendLine($"readiness_evidence={evidence.ReadinessEvidencePath}");
        if (!string.IsNullOrWhiteSpace(evidence.VerifyReportPath))
            sb.AppendLine($"verify_report={evidence.VerifyReportPath}");

        if (_verifyEvidence is not null)
            return VerifyRepairEvidenceFormatter.EnrichWithArtifactPaths(sb.ToString().TrimEnd(), id, _verifyEvidence);

        return sb.ToString().TrimEnd();
    }

    private static string BuildObjective(string? playbookHint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fix the shadow workspace build/test failures.");
        sb.AppendLine("Investigate CONTEXT FRAGMENTS below with inspect_environment/read_file/grep/list_directory.");
        sb.AppendLine("Apply minimal fixes via edit_file/apply_patch.");
        sb.AppendLine("Verify: run_build for compile/install errors; run_tests when tests fail but build passes.");

        if (!string.IsNullOrWhiteSpace(playbookHint))
            sb.AppendLine($"Prior successful fix pattern available in fragments (playbook source).");

        return sb.ToString().TrimEnd();
    }
}
