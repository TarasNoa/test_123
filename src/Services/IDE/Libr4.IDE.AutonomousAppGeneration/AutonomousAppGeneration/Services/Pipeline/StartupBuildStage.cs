using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class StartupBuildStage : IGenerationStage
{
    public string Name => "startup_build";
    public int Order => 300;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        context.Items["startup_build_stage_reached"] = true;
        return Task.FromResult(PipelineStageHelper.MarkAndContinue(
            context.Orchestrator,
            AutonomousPipelineStages.StartupBuild));
    }
}
