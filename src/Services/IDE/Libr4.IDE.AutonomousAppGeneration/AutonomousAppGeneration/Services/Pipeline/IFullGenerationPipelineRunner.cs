using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public interface IFullGenerationPipelineRunner
{
    Task<PipelineRunOutcome> RunStageAsync(GenerationContext context, string stageName, CancellationToken ct);
    Task<PipelineRunOutcome> RunPostPlanningAsync(GenerationContext context, CancellationToken ct);
}

public sealed class FullGenerationPipelineRunner : IFullGenerationPipelineRunner
{
    private readonly IReadOnlyList<IGenerationStage> _stages;
    private readonly ILogger<FullGenerationPipelineRunner> _logger;

    public FullGenerationPipelineRunner(
        IEnumerable<IGenerationStage> stages,
        ILogger<FullGenerationPipelineRunner> logger)
    {
        _stages = stages
            .Where(s => s.Order >= 200)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
        _logger = logger;
    }

    public Task<PipelineRunOutcome> RunStageAsync(GenerationContext context, string stageName, CancellationToken ct) =>
        RunStagesAsync(context, _stages.Where(s => s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase)), ct);

    public Task<PipelineRunOutcome> RunPostPlanningAsync(GenerationContext context, CancellationToken ct) =>
        RunStagesAsync(context, _stages, ct);

    private async Task<PipelineRunOutcome> RunStagesAsync(
        GenerationContext context,
        IEnumerable<IGenerationStage> stages,
        CancellationToken ct)
    {
        var executed = new List<string>();
        foreach (var stage in stages)
        {
            ct.ThrowIfCancellationRequested();
            StageOutcome outcome;
            try
            {
                outcome = await stage.ExecuteAsync(context, ct).ConfigureAwait(false);
                executed.Add(stage.Name);
            }
            catch (Exception ex)
            {
                var reason = $"stage_exception:{stage.Name}:{ex.GetType().Name}:{ex.Message}";
                _logger.LogError(ex, "[FullPipeline] Stage {Stage} threw", stage.Name);
                context.FailureReason ??= reason;
                return new PipelineRunOutcome(false, reason, stage.Name, false, executed);
            }

            if (outcome.ShortCircuit)
                return new PipelineRunOutcome(true, null, null, true, executed);

            if (!outcome.ShouldContinue)
            {
                var reason = outcome.FailureReason ?? $"stage_failed:{stage.Name}";
                context.FailureReason ??= reason;
                return new PipelineRunOutcome(false, reason, stage.Name, false, executed);
            }
        }

        return new PipelineRunOutcome(true, null, null, false, executed);
    }
}
