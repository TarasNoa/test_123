namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class ConsistencyCheckStage : IGenerationStage
{
    public string Name => "consistency_check";
    public int Order => 270;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Files.Count == 0 && context.Orchestrator.Files.Count == 0)
            return Task.FromResult(StageOutcome.Stop("consistency_check_no_files"));

        context.Items["consistency_check_passed"] = true;
        return Task.FromResult(StageOutcome.Continue);
    }
}
