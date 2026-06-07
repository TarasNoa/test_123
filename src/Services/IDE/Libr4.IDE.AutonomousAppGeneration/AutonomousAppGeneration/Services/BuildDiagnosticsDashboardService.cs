using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IBuildDiagnosticsDashboardService
{
    BuildDiagnosticsDashboardDto Build(
        AppGenerationOrchestrator orchestrator,
        RunQualityAssessmentDto quality,
        VerifyRecipeDetectionResult? verifyRecipe = null,
        string? stackFilter = null);
}

public sealed class BuildDiagnosticsDashboardService : IBuildDiagnosticsDashboardService
{
    private readonly IVerifyEvidenceStore _verifyEvidence;
    private readonly IObscuraEvidenceStore? _obscuraEvidence;

    public BuildDiagnosticsDashboardService(
        IVerifyEvidenceStore verifyEvidence,
        IObscuraEvidenceStore? obscuraEvidence = null)
    {
        _verifyEvidence = verifyEvidence;
        _obscuraEvidence = obscuraEvidence;
    }

    public BuildDiagnosticsDashboardDto Build(
        AppGenerationOrchestrator orchestrator,
        RunQualityAssessmentDto quality,
        VerifyRecipeDetectionResult? verifyRecipe = null,
        string? stackFilter = null)
    {
        var allGates = orchestrator.QualityGates
            .Select((g, idx) => MapTimelineEntry(idx + 1, g))
            .ToList();

        var stackFilters = StackQualityGateFilter.BuildOptions(allGates, verifyRecipe);
        var activeFilter = string.IsNullOrWhiteSpace(stackFilter) ? "all" : stackFilter.Trim();
        var gates = StackQualityGateFilter.Apply(allGates, activeFilter);

        var phases = gates
            .GroupBy(g => g.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var evals = g.Count();
                var passed = g.Count(x => x.Passed);
                var latest = g.OrderBy(x => x.Sequence).Last();
                var topFailures = g.Where(x => !x.Passed)
                    .SelectMany(x => x.Reasons)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(x => x.Count())
                    .Take(5)
                    .Select(x => x.Key)
                    .ToList();
                return new BuildPhaseDiagnosticsDto(
                    Category: g.Key,
                    Evaluations: evals,
                    Passed: passed,
                    Failed: evals - passed,
                    LatestScore: latest.Score,
                    PassRate: evals == 0 ? 0 : Math.Round((double)passed / evals, 4),
                    TopFailureReasons: topFailures);
            })
            .OrderByDescending(p => p.Evaluations)
            .ThenBy(p => p.Category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var repairTiers = gates
            .GroupBy(g => g.Tier, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RepairTierDiagnosticsDto(
                Tier: g.Key,
                GateHits: g.Count(),
                FailedGateHits: g.Count(x => !x.Passed),
                Stages: g.Select(x => x.Stage).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList()))
            .OrderBy(t => TierOrder(t.Tier))
            .ToList();

        var total = gates.Count;
        var passed = gates.Count(g => g.Passed);
        var failed = total - passed;
        var failedIterations = orchestrator.Iterations.Count(i => !i.Succeeded);
        var weakest = phases.Where(p => p.Failed > 0).OrderBy(p => p.PassRate).ThenBy(p => p.LatestScore).FirstOrDefault();
        var strongest = phases.Where(p => p.Passed > 0).OrderByDescending(p => p.PassRate).ThenByDescending(p => p.LatestScore).FirstOrDefault();

        var detectedStack = orchestrator.Plan is not null
            ? StackArtifactRecoveryRouter.DescribeStack(orchestrator.Plan)
            : "Unknown";

        var ecosystemMatches = orchestrator.Plan is not null
            ? StackArtifactRecoveryRouter.MatchEcosystems(orchestrator.Plan, orchestrator.Files)
            : Array.Empty<EcosystemMatch>();

        var detectedEcosystems = ecosystemMatches
            .Select(m => new DetectedEcosystemDto(
                m.Profile.Id,
                m.Profile.DisplayName,
                m.Profile.Category.ToString(),
                m.Score,
                m.Reasons))
            .ToList();

        var summary = new BuildDiagnosticsSummaryDto(
            DetectedStack: detectedStack,
            DetectedEcosystems: detectedEcosystems,
            CatalogLanguageCount: DeveloperEcosystemCatalog.LanguageCount,
            CatalogFrameworkCount: DeveloperEcosystemCatalog.FrameworkCount,
            TotalGates: total,
            PassedGates: passed,
            FailedGates: failed,
            PassRate: total == 0 ? 0 : Math.Round((double)passed / total, 4),
            IterationCount: orchestrator.Iterations.Count,
            FailedIterations: failedIterations,
            FileCount: orchestrator.Files.Count,
            OverallQualityScore: quality.OverallScore,
            QualityVerdict: quality.Verdict,
            WeakestPhase: weakest?.Category,
            StrongestPhase: strongest?.Category);

        var recoveryEfficiency = RecoveryEfficiencyAggregator.BuildReport(orchestrator);
        var verifyEvidence = MapVerifyEvidence(_verifyEvidence.List(orchestrator.Id));
        var obscuraEvidence = _obscuraEvidence is null
            ? null
            : MapObscuraEvidence(_obscuraEvidence.List(orchestrator.Id));
        var verifyRecipeDto = verifyRecipe is null
            ? null
            : new VerifyRecipeDashboardDto(
                verifyRecipe.Recipe.Id,
                verifyRecipe.Recipe.DisplayName,
                verifyRecipe.DetectionMethod,
                verifyRecipe.Recipe.BuildCommands,
                verifyRecipe.Recipe.TestCommands,
                verifyRecipe.Recipe.SmokeKind.ToString());

        return new BuildDiagnosticsDashboardDto(
            RunId: orchestrator.Id,
            Status: orchestrator.Status.ToString(),
            ApplicationName: orchestrator.Plan?.ApplicationName,
            FailureReason: orchestrator.FailureReason,
            GeneratedAtUtc: DateTime.UtcNow,
            Summary: summary,
            Timeline: gates,
            Phases: phases,
            RepairTiers: repairTiers,
            RecoveryEfficiency: recoveryEfficiency,
            Recommendations: BuildRecommendations(orchestrator, phases, summary, recoveryEfficiency),
            VerifyEvidence: verifyEvidence,
            ObscuraEvidence: obscuraEvidence,
            VerifyRecipe: verifyRecipeDto,
            StackFilters: stackFilters,
            ActiveStackFilter: activeFilter);
    }

    private static ObscuraEvidenceDiagnosticsDto? MapObscuraEvidence(ObscuraEvidenceBundle bundle)
    {
        if (!bundle.DirectoryExists && bundle.Artifacts.Count == 0)
            return null;

        return new ObscuraEvidenceDiagnosticsDto(
            bundle.ObscuraDirectory,
            bundle.VerifyDirectory,
            bundle.DirectoryExists,
            bundle.ThumbnailUrl,
            bundle.ManifestPath is null
                ? null
                : $"/api/ide/app-generation/{bundle.RunId:D}/obscura/artifacts/manifest.json",
            bundle.Artifacts.Select(a => new ObscuraEvidenceArtifactDto(
                a.Kind.ToString(),
                a.FileName,
                a.ContentHash,
                a.LogicalName,
                a.StepNumber,
                a.ToolName,
                a.DownloadUrl,
                a.ThumbnailUrl,
                a.SizeBytes,
                a.LastModifiedUtc,
                a.ContentType)).ToList());
    }

    private static VerifyEvidenceDiagnosticsDto? MapVerifyEvidence(VerifyEvidenceBundle bundle)
    {
        if (!bundle.DirectoryExists && bundle.Artifacts.Count == 0)
            return null;

        return new VerifyEvidenceDiagnosticsDto(
            bundle.EvidenceDirectory,
            bundle.DirectoryExists,
            bundle.ThumbnailUrl,
            bundle.Artifacts.Select(a => new VerifyEvidenceArtifactDto(
                a.Kind.ToString(),
                a.FileName,
                a.DownloadUrl,
                a.ThumbnailUrl,
                a.SizeBytes,
                a.LastModifiedUtc,
                a.ContentType)).ToList());
    }

    private static BuildGateTimelineEntryDto MapTimelineEntry(int sequence, QualityGateSnapshot gate)
    {
        var category = CategorizeStage(gate.Stage);
        var tier = MapCategoryToTier(category, gate.Stage);
        return new BuildGateTimelineEntryDto(
            Sequence: sequence,
            Stage: gate.Stage,
            Category: category,
            Tier: tier,
            Score: gate.Score,
            Passed: gate.Passed,
            Reasons: gate.Reasons.ToList(),
            EvaluatedAtUtc: gate.EvaluatedAtUtc);
    }

    private static string CategorizeStage(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return "unknown";

        var s = stage.ToLowerInvariant();
        if (s.Contains("pre_safety") || s.Contains("artifact_normalization") || s.Contains("structural"))
            return "normalization";
        if (s.Contains("repair_error_classifier") || s.Contains("runtime_recovery") || s.Contains("repair_")
            || s.Contains("fix") || s.Contains("root_cause"))
            return "recovery";
        if (s.StartsWith("build") || s.Contains("startup_build") || s.Contains("phase_compile"))
            return "build";
        if (s.Contains("execution") || s.Contains("test"))
            return "execution";
        if (s.Contains("review") || s.Contains("security"))
            return "review";
        if (s.Contains("plan") || s.Contains("generation") || s.Contains("consistency"))
            return "generation";
        return "other";
    }

    private static string MapCategoryToTier(string category, string stage)
    {
        if (stage.Contains("repair_error_classifier", StringComparison.OrdinalIgnoreCase))
            return "L0-L4 classifier";
        if (stage.Contains("runtime_recovery", StringComparison.OrdinalIgnoreCase))
            return "L3 runtime";
        return category switch
        {
            "normalization" => "L0 structural",
            "build" => "L1 build",
            "execution" => "L2 compile",
            "recovery" => "L0-L2 recovery",
            "review" => "L4 business",
            _ => "meta"
        };
    }

    private static int TierOrder(string tier) => tier switch
    {
        "L0 structural" => 0,
        "L0-L4 classifier" => 1,
        "L1 build" => 2,
        "L2 compile" => 3,
        "L3 runtime" => 4,
        "L0-L2 recovery" => 5,
        "L4 business" => 6,
        _ => 9
    };

    private static IReadOnlyList<string> BuildRecommendations(
        AppGenerationOrchestrator orchestrator,
        IReadOnlyList<BuildPhaseDiagnosticsDto> phases,
        BuildDiagnosticsSummaryDto summary,
        RecoveryEfficiencyReportDto recovery)
    {
        var list = new List<string>();
        if (summary.FailedGates > 0 && phases.Any(p => p.Category == "build" && p.Failed > 0))
            list.Add("Build phase failed: inspect timeline build/startup_build gates and apply Level 0 POM validation before Maven.");
        if (phases.Any(p => p.Category == "normalization" && p.Failed > 0))
            list.Add("Artifact contamination detected: run pre-safety normalization and JWT stack consolidation.");
        if (phases.Any(p => p.Category == "recovery" && p.Failed > 0))
            list.Add("Repair loop exhausted: check repair_error_classifier and runtime_recovery_l3 gates for deterministic vs LLM routing.");
        var zeroPatch = recovery.Events.Count(e => e.PatchesApplied == 0);
        if (recovery.TotalAttempts > 0 && zeroPatch >= recovery.TotalAttempts * 0.5)
            list.Add($"Recovery Efficiency: {zeroPatch}/{recovery.TotalAttempts} attempts produced zero patches — prioritize ManifestRepairEngine and DependencySyncEngine.");
        if (recovery.TotalAttempts > 0 && recovery.LlmAttemptShare >= 0.5)
            list.Add($"Recovery Efficiency: LLM used in {recovery.LlmAttemptShare:P0} of repair attempts — strengthen root-cause deterministic coverage before adding framework handlers.");
        if (recovery.TotalAttempts >= 3 && recovery.ByRootCause.FirstOrDefault()?.Resolved == 0)
            list.Add($"Root cause '{recovery.ByRootCause[0].Category}' repeats without resolution — prioritize this category, not catalog expansion.");
        if (!recovery.RecoveryMeasurementEligible)
            list.Add(recovery.RecoveryMeasurementSummary
                     ?? $"Recovery was not measured (pipeline stopped before {AutonomousPipelineStages.StartupBuild}).");
        else if (recovery.TotalAttempts == 0)
            list.Add(recovery.RecoveryMeasurementSummary
                     ?? "Recovery eligible but no repair attempts recorded — check build gate or iteration budget.");
        if (string.Equals(orchestrator.Status.ToString(), "Failed", StringComparison.OrdinalIgnoreCase)
            && orchestrator.FailureReason?.Contains("iteration_budget", StringComparison.OrdinalIgnoreCase) == true)
            list.Add("Iteration budget exceeded: enable early structural validation or reduce duplicate artifact passes.");
        if (list.Count == 0 && summary.PassRate >= 0.9)
            list.Add("Gate health is strong; focus on execution/runtime stability if builds are green.");
        return list;
    }
}
