using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class GenerationStage : IGenerationStage
{
    private readonly ILogger<GenerationStage> _logger;

    public GenerationStage(ILogger<GenerationStage> logger) => _logger = logger;

    public string Name => "generation";
    public int Order => 200;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Plan is null)
            return Task.FromResult(StageOutcome.Stop("plan_missing_for_generation"));

        context.Items["generation_stage_reached"] = true;
        if (context.Files.Count == 0 && context.PhaseBatches is null or { Count: 0 })
            _logger.LogDebug("[Pipeline] Generation stage marker — files produced by host orchestrator.");

        return Task.FromResult(PipelineStageHelper.MarkAndContinue(context.Orchestrator, AutonomousPipelineStages.Generation));
    }
}
