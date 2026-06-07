using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifySubagentService : IVerifySubagentService
{
    private readonly IVerifyRecipeRegistry _recipeRegistry;
    private readonly IVerifyOrchestrator _orchestrator;
    private readonly IVerifyGateService _gate;
    private readonly IVerifyFailureContextStore _failureStore;
    private readonly IVerifyEvidenceStore _evidenceStore;
    private readonly VerifySubagentOptions _options;
    private readonly AutonomousBenchmarkModeOptions _benchmarkOptions;
    private readonly AutonomousPlatformUtilizationOptions _platformOptions;
    private readonly IVerifyPassCheckpointService? _verifyCheckpoint;
    private readonly ILogger<VerifySubagentService> _logger;

    public VerifySubagentService(
        IVerifyRecipeRegistry recipeRegistry,
        IVerifyOrchestrator orchestrator,
        IVerifyGateService gate,
        IVerifyFailureContextStore failureStore,
        IVerifyEvidenceStore evidenceStore,
        IOptions<VerifySubagentOptions> options,
        IOptions<AutonomousBenchmarkModeOptions> benchmarkOptions,
        IOptions<AutonomousPlatformUtilizationOptions> platformOptions,
        ILogger<VerifySubagentService> logger,
        IVerifyPassCheckpointService? verifyCheckpoint = null)
    {
        _recipeRegistry = recipeRegistry;
        _orchestrator = orchestrator;
        _gate = gate;
        _failureStore = failureStore;
        _evidenceStore = evidenceStore;
        _options = options.Value;
        _benchmarkOptions = benchmarkOptions.Value;
        _platformOptions = platformOptions.Value;
        _logger = logger;
        _verifyCheckpoint = verifyCheckpoint;
    }

    public async Task<VerifySubagentResult> RunAsync(GenerationContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.Enabled)
        {
            return new VerifySubagentResult(
                Passed: true,
                Summary: "verify disabled",
                EvidencePath: null,
                Skipped: true,
                SkipReason: "disabled");
        }

        if (BenchmarkExecutionPathPolicy.ShouldDeferFailedGate(
                _benchmarkOptions,
                BenchmarkExecutionPathPolicy.Stages.Verify,
                _platformOptions))
        {
            return new VerifySubagentResult(
                Passed: true,
                Summary: "verify skipped in benchmark mode",
                EvidencePath: null,
                Skipped: true,
                SkipReason: "benchmark_optional");
        }

        var testsGreen = context.Items.TryGetValue("tests_passed", out var testsFlag) && testsFlag is true;
        if (!testsGreen && context.Orchestrator.Status != GenerationStatus.Completed)
        {
            return new VerifySubagentResult(
                Passed: false,
                Summary: "verify blocked: tests not green",
                EvidencePath: null);
        }

        if (context.Plan is null)
        {
            return new VerifySubagentResult(
                Passed: false,
                Summary: "verify blocked: plan missing",
                EvidencePath: null);
        }

        var evidenceDir = Path.Combine(
            _options.EvidenceRoot,
            context.Orchestrator.Id.ToString("D"),
            "verify");
        Directory.CreateDirectory(evidenceDir);

        var recipeDetection = await _recipeRegistry.DetectAsync(
            new VerifyRecipeDetectionRequest(
                context.Orchestrator.Files,
                context.Plan,
                context.UserRequest,
                context.Orchestrator.Id,
                _options.EvidenceRoot),
            ct).ConfigureAwait(false);

        var runPlan = _orchestrator.PrepareVerifyRun(context, recipeDetection, evidenceDir);
        var orchestration = await _orchestrator.RunVerifyOrchestrationAsync(context, runPlan, ct)
            .ConfigureAwait(false);
        var gate = _gate.Evaluate(orchestration, runPlan);

        var report = BuildReport(context, recipeDetection, runPlan, orchestration, gate);
        var evidencePath = Path.Combine(evidenceDir, "verify-report.json");
        var payload = new
        {
            runId = context.Orchestrator.Id,
            passed = gate.Passed,
            recipeId = recipeDetection.Recipe.Id,
            detectionMethod = recipeDetection.DetectionMethod,
            shadowPassed = orchestration.ShadowPassed,
            readinessPassed = orchestration.ReadinessPassed,
            agentPassed = orchestration.AgentPassed,
            agentSummary = orchestration.AgentSummary,
            failureReasons = gate.FailureReasons,
            readinessEvidencePath = orchestration.ReadinessEvidencePath,
            failureEvidencePath = orchestration.FailureEvidencePath,
            summary = report,
            completedAtUtc = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(
            evidencePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);

        if (!gate.Passed)
        {
            var enrichedReport = VerifyRepairEvidenceFormatter.EnrichWithArtifactPaths(
                report,
                context.Orchestrator.Id,
                _evidenceStore);
            var repairEvidence = new VerifyFailureEvidence(
                context.Orchestrator.Id,
                recipeDetection.Recipe.Id,
                gate.Summary,
                enrichedReport,
                orchestration.ReadinessEvidencePath,
                evidencePath,
                DateTime.UtcNow);
            _failureStore.Set(context.Orchestrator.Id, repairEvidence);
            context.Items["verify_repair_evidence"] = repairEvidence.Summary + "\n" + enrichedReport;
        }

        context.Items["verify_passed"] = gate.Passed;
        context.Items["verify_evidence_path"] = evidencePath;
        context.Items["verify_summary"] = report;
        context.Items["verify_recipe_id"] = recipeDetection.Recipe.Id;
        context.Items["verify_recipe_manifest"] = recipeDetection.ManifestPath;
        context.Items["verify_readiness_path"] = orchestration.ReadinessEvidencePath;
        context.Items["verify_failure_evidence_path"] = orchestration.FailureEvidencePath;

        if (gate.Passed && _verifyCheckpoint is not null)
        {
            await _verifyCheckpoint.RecordVerifyPassAsync(
                context.Orchestrator.Id,
                context.Orchestrator.ShadowWorkspaceId,
                ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "[Verify {RunId}] Gate passed={Passed} shadow={Shadow} readiness={Readiness} agent={Agent}",
            context.Orchestrator.Id,
            gate.Passed,
            orchestration.ShadowPassed,
            orchestration.ReadinessPassed,
            orchestration.AgentPassed);

        return new VerifySubagentResult(gate.Passed, report, evidencePath);
    }

    private static string BuildReport(
        GenerationContext context,
        VerifyRecipeDetectionResult recipeDetection,
        VerifyRunPlan runPlan,
        VerifyOrchestrationResult orchestration,
        VerifyGateResult gate)
    {
        var report = new StringBuilder();
        report.AppendLine($"run_id={context.Orchestrator.Id:D}");
        report.AppendLine($"app={context.Plan!.ApplicationName}");
        report.AppendLine($"status={context.Orchestrator.Status}");
        report.AppendLine($"recipe={recipeDetection.Recipe.Id} ({recipeDetection.DetectionMethod})");
        if (recipeDetection.ManifestPath is not null)
            report.AppendLine($"manifest={recipeDetection.ManifestPath}");
        report.AppendLine($"shadow_verify={(orchestration.ShadowPassed ? "pass" : "fail")}");
        report.AppendLine($"readiness_verify={(orchestration.ReadinessPassed ? "pass" : "fail")}");
        foreach (var readiness in orchestration.ReadinessResults)
            report.AppendLine($"- {readiness.TargetName} {readiness.Url} => {(readiness.Ready ? "ready" : "not_ready")}");
        report.AppendLine($"agent_verify={(orchestration.AgentPassed ? "pass" : "fail")}");
        report.AppendLine(orchestration.AgentSummary);
        report.AppendLine($"gate={(gate.Passed ? "pass" : "fail")}");
        report.AppendLine(gate.Summary);
        return report.ToString();
    }
}
