using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class SecurityReviewStage : IGenerationStage
{
    public string Name => "security_review";
    public int Order => 250;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        context.Items["security_review_stage_reached"] = true;
        return Task.FromResult(PipelineStageHelper.MarkAndContinue(
            context.Orchestrator,
            AutonomousPipelineStages.Security));
    }
}
