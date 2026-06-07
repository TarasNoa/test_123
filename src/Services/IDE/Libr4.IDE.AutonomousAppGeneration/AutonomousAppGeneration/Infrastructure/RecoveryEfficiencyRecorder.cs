using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public static class RecoveryEfficiencyRecorder
{
    public static RecoveryMechanism ResolveMechanism(
        bool usedLevel0,
        bool usedLevel3,
        bool usedDeterministicCompile,
        bool usedLlm,
        bool usedEscalation = false,
        bool usedSurgicalLlm = false,
        bool usedAgentRuntime = false)
    {
        if (usedEscalation)
            return RecoveryMechanism.RootCauseEscalation;
        if (usedLevel0)
            return RecoveryMechanism.DeterministicStructural;
        if (usedLevel3)
            return RecoveryMechanism.DeepStackHandler;
        if (usedAgentRuntime)
            return RecoveryMechanism.AgentToolLoop;
        if (usedSurgicalLlm)
            return RecoveryMechanism.SurgicalLlm;
        if (usedLlm)
            return RecoveryMechanism.Llm;
        if (usedDeterministicCompile)
            return RecoveryMechanism.DeterministicCompile;
        return RecoveryMechanism.None;
    }

    public static void RecordAttempt(
        AppGenerationOrchestrator orchestrator,
        int iterationNumber,
        CompileRepairPlanner.RepairPlan plan,
        IReadOnlyList<RepairErrorClassifier.ClassifiedError> classified,
        RecoveryMechanism mechanism,
        IReadOnlyList<GeneratedFile> patches,
        string? errorSignature)
    {
        var primaryClass = classified.FirstOrDefault()?.Class.ToString() ?? "Unknown";
        var fromClassifier = classified.Count > 0
            ? RecoveryRootCauseMapper.FromClassifier(
                classified[0].Class,
                plan.RootCause.FilePath,
                plan.RootCause.Message,
                plan.SymbolAnalysis)
            : RecoveryRootCauseCategory.Unknown;
        var fromPlanner = plan.SymbolAnalysis is not null
            ? RecoveryRootCauseMapper.FromCompileFixKind(plan.SymbolAnalysis.Kind)
            : RecoveryRootCauseMapper.FromPlannerCategory(plan.RootCauseCategory);
        var rootCause = RecoveryRootCauseMapper.Merge(fromClassifier, fromPlanner);

        orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
            iterationNumber,
            rootCause,
            primaryClass,
            mechanism,
            patches.Count,
            BuildSucceededAfterRepair: null,
            DateTime.UtcNow,
            errorSignature));
    }

    public static void ClosePendingOutcome(AppGenerationOrchestrator orchestrator, bool buildSucceeded) =>
        orchestrator.FinalizeLastRecoveryOutcome(buildSucceeded);
}
