using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

internal static class PipelineStageHelper
{
    public static StageOutcome MarkAndContinue(AppGenerationOrchestrator orchestrator, string stage) =>
        Mark(orchestrator, stage, shouldContinue: true);

    public static StageOutcome MarkAndStop(AppGenerationOrchestrator orchestrator, string stage, string reason)
    {
        orchestrator.RecordPipelineStageReached(stage);
        return StageOutcome.Stop(reason);
    }

    private static StageOutcome Mark(AppGenerationOrchestrator orchestrator, string stage, bool shouldContinue)
    {
        orchestrator.RecordPipelineStageReached(stage);
        return shouldContinue ? StageOutcome.Continue : StageOutcome.Stop("stopped");
    }
}
