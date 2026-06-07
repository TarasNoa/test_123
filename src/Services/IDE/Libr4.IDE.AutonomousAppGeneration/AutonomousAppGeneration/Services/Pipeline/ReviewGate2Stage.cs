using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class ReviewGate2Stage : IGenerationStage
{
    public string Name => "review_gate_2";
    public int Order => 260;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        context.Items["review_gate_2_stage_reached"] = true;
        return Task.FromResult(PipelineStageHelper.MarkAndContinue(
            context.Orchestrator,
            AutonomousPipelineStages.ReviewGate2));
    }
}
