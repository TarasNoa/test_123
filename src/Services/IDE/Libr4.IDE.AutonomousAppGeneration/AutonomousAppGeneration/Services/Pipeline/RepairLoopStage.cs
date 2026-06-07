using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class RepairLoopStage : IGenerationStage
{
    public string Name => "repair_loop";
    public int Order => 400;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        context.Items["repair_loop_stage_reached"] = true;
        return Task.FromResult(PipelineStageHelper.MarkAndContinue(
            context.Orchestrator,
            AutonomousPipelineStages.RepairLoop));
    }
}
