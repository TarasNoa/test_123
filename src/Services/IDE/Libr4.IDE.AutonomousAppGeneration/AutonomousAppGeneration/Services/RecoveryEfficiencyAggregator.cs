using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public static class RecoveryEfficiencyAggregator
{
    public static RecoveryEfficiencyReportDto BuildReport(AppGenerationOrchestrator orchestrator)
    {
        var events = orchestrator.RecoveryEfficiencyRecords;
        var attempts = events.Count;
        var resolved = events.Count(e => e.BuildSucceededAfterRepair == true);
        var failed = events.Count(e => e.BuildSucceededAfterRepair == false);
        var pending = events.Count(e => e.BuildSucceededAfterRepair is null);

        var byMechanism = events
            .GroupBy(e => e.Mechanism)
            .Select(g => new RecoveryMechanismShareDto(
                g.Key.ToString(),
                g.Count(),
                Share(g.Count(), attempts),
                g.Count(e => e.BuildSucceededAfterRepair == true),
                g.Count(e => e.BuildSucceededAfterRepair == false)))
            .OrderByDescending(x => x.Attempts)
            .ToList();

        var byRootCause = events
            .GroupBy(e => e.RootCauseCategory)
            .Select(g => new RecoveryRootCauseShareDto(
                g.Key.ToString(),
                g.Count(),
                Share(g.Count(), attempts),
                g.Count(e => e.BuildSucceededAfterRepair == true)))
            .OrderByDescending(x => x.Attempts)
            .ToList();

        var recoverySource = events
            .GroupBy(e => MapRecoverySource(e.Mechanism))
            .Select(g => new RecoverySourceShareDto(
                g.Key,
                g.Count(),
                Share(g.Count(), attempts),
                g.Count(e => e.BuildSucceededAfterRepair == true)))
            .OrderByDescending(x => x.Attempts)
            .ToList();

        var timeline = events
            .Select(e => new RecoveryEfficiencyEventDto(
                e.IterationNumber,
                e.RootCauseCategory.ToString(),
                e.PrimaryErrorClass,
                e.Mechanism.ToString(),
                e.PatchesApplied,
                e.BuildSucceededAfterRepair,
                e.AttemptedAtUtc,
                e.ErrorSignature))
            .ToList();

        var llmEvents = events.Where(e => e.Mechanism == RecoveryMechanism.Llm).ToList();
        var llmResolved = llmEvents.Count(e => e.BuildSucceededAfterRepair == true);
        var llmFailed = llmEvents.Count(e => e.BuildSucceededAfterRepair == false);
        var llmStats = new LlmRecoveryStatsDto(
            llmEvents.Count,
            llmResolved,
            llmFailed,
            llmEvents.Count == 0 ? 0 : Math.Round((double)llmResolved / llmEvents.Count, 4));

        var repeated = events
            .Where(e => !string.IsNullOrWhiteSpace(e.ErrorSignature))
            .GroupBy(e => e.ErrorSignature!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RepeatedFailureDto(
                g.Key,
                g.Count(),
                g.Count(e => e.BuildSucceededAfterRepair == true)))
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToList();

        var timeLost = events
            .Where(e => e.RepairDurationMs is > 0)
            .GroupBy(e => e.RootCauseCategory)
            .Select(g =>
            {
                var totalMs = g.Sum(e => e.RepairDurationMs ?? 0);
                var count = g.Count();
                return new TimeLostByCategoryDto(
                    g.Key.ToString(),
                    count,
                    Math.Round(totalMs / 60000.0, 2),
                    Math.Round(totalMs / 60000.0 / count, 2));
            })
            .OrderByDescending(x => x.TotalMinutes)
            .ToList();

        var llmAttempts = events.Count(e => e.Mechanism == RecoveryMechanism.Llm);
        var deterministicAttempts = events.Count(e => MapRecoverySource(e.Mechanism) is "Deterministic" or "Pattern" or "DeepHandler");

        FirstFailureReportDto? firstFailure = null;
        if (orchestrator.FirstFailure is { } ff)
        {
            firstFailure = new FirstFailureReportDto(
                ff.ErrorClass,
                ff.RootCauseCategory,
                ff.IterationNumber,
                ff.RecoveredAfterRepair);
        }

        var firstFailureReason = firstFailure is not null
            ? $"{firstFailure.ErrorClass}:{firstFailure.RootCauseCategory}"
            : null;
        var lastFailureReason = orchestrator.Status == GenerationStatus.Failed
            ? orchestrator.FailureReason
            : attempts > 0
                ? events.LastOrDefault()?.PrimaryErrorClass
                : null;

        var recoveryEligible = AutonomousPipelineStages.IsRecoveryMeasurementEligible(orchestrator.PipelineStageReached);
        var recoverySummary = BuildRecoveryMeasurementSummary(
            recoveryEligible,
            orchestrator.PipelineStageReached,
            attempts);

        var patchesApplied = events.Sum(e => e.PatchesApplied);

        return new RecoveryEfficiencyReportDto(
            TotalAttempts: attempts,
            ResolvedAttempts: resolved,
            FailedAttempts: failed,
            PendingOutcome: pending,
            PatchesApplied: patchesApplied,
            LlmAttemptShare: Share(llmAttempts, attempts),
            DeterministicAttemptShare: Share(deterministicAttempts, attempts),
            ByMechanism: byMechanism,
            ByRootCause: byRootCause,
            RecoverySource: recoverySource,
            FirstFailure: firstFailure,
            PipelineStageReached: orchestrator.PipelineStageReached,
            FirstFailureReason: firstFailureReason,
            LastFailureReason: lastFailureReason,
            RecoveryMeasurementEligible: recoveryEligible,
            RecoveryMeasurementSummary: recoverySummary,
            LlmStats: llmStats,
            RepeatedErrors: repeated,
            TimeLostByRootCause: timeLost,
            Events: timeline,
            Insight: BuildInsight(
                orchestrator,
                recoveryEligible,
                recoverySummary,
                recoverySource,
                byRootCause,
                attempts,
                llmStats));
    }

    private static string BuildRecoveryMeasurementSummary(
        bool eligible,
        string? pipelineStageReached,
        int attempts)
    {
        if (eligible && attempts > 0)
            return "Recovery measured: startup build reached and repair loop recorded attempts.";

        if (eligible)
            return "Recovery eligible (StartupBuild+), but no repair attempts were recorded yet.";

        var stage = pipelineStageReached ?? "Unknown";
        return $"Recovery was not measured. Pipeline stopped before {AutonomousPipelineStages.StartupBuild} (reached: {stage}).";
    }

    private static string MapRecoverySource(RecoveryMechanism mechanism) => mechanism switch
    {
        RecoveryMechanism.Llm => "LLM",
        RecoveryMechanism.DeepStackHandler or RecoveryMechanism.RootCauseEscalation => "DeepHandler",
        RecoveryMechanism.PatternRecovery => "Pattern",
        RecoveryMechanism.DeterministicStructural
            or RecoveryMechanism.DeterministicRuntime
            or RecoveryMechanism.DeterministicCompile => "Deterministic",
        _ => "Other"
    };

    private static string BuildInsight(
        AppGenerationOrchestrator orchestrator,
        bool recoveryMeasurementEligible,
        string recoveryMeasurementSummary,
        IReadOnlyList<RecoverySourceShareDto> recoverySource,
        IReadOnlyList<RecoveryRootCauseShareDto> byRootCause,
        int attempts,
        LlmRecoveryStatsDto llmStats)
    {
        if (!recoveryMeasurementEligible)
            return recoveryMeasurementSummary;

        if (attempts == 0)
        {
            var stage = orchestrator.PipelineStageReached ?? "Unknown";
            return stage == AutonomousPipelineStages.RepairLoop || stage == AutonomousPipelineStages.Completed
                ? "Repair loop reached; repair metrics not recorded yet."
                : recoveryMeasurementSummary;
        }

        var zeroPatchAttempts = orchestrator.RecoveryEfficiencyRecords.Count(e => e.PatchesApplied == 0);
        if (zeroPatchAttempts > 0 && zeroPatchAttempts >= attempts * 0.5)
        {
            return $"Recovery bottleneck: {zeroPatchAttempts}/{attempts} repair attempts with patchesApplied=0. "
                   + "Усилите ManifestRepairEngine / DependencySyncEngine / StackPurity до оптимизации LLM.";
        }

        var topSource = recoverySource.FirstOrDefault();
        var topCause = byRootCause.FirstOrDefault();
        var status = orchestrator.Status.ToString();
        return $"Статус={status}; repair={attempts}; топ-источник={topSource?.Source} ({topSource?.Share:P0}); "
               + $"топ-root-cause={topCause?.Category}; LLM success={llmStats.SuccessRate:P0} ({llmStats.Resolved}/{llmStats.Invoked}). "
               + "Инвестируйте в Configuration/MissingType/Imports/Dependencies, а не в framework handlers.";
    }

    private static double Share(int part, int total) =>
        total == 0 ? 0 : Math.Round((double)part / total, 4);
}
