using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 of audit roadmap. Orchestrates a sequence of <see cref="IGenerationStage"/>
/// implementations against a single <see cref="GenerationContext"/>.
///
/// Semantics:
///   * Stages run in ascending <see cref="IGenerationStage.Order"/>; ties broken by name (ordinal).
///   * <see cref="StageOutcome.ShortCircuit"/> halts execution as a successful early exit.
///   * <see cref="StageOutcome.ShouldContinue"/> false (without ShortCircuit) halts as a hard failure;
///     <c>FailureReason</c> propagates to <see cref="GenerationContext.FailureReason"/>.
///   * Exceptions in a stage are caught, surface as <c>FailureReason</c> = "stage_exception:{Name}:{message}",
///     and halt the pipeline (no rethrow) so callers see deterministic outcomes.
///   * Each stage gets its own logger scope tagged with stage name + run id.
/// </summary>
public interface IGenerationPipelineRunner
{
    Task<PipelineRunOutcome> RunAsync(GenerationContext context, CancellationToken ct);
}

public sealed record PipelineRunOutcome(
    bool Succeeded,
    string? FailureReason,
    string? FailedStageName,
    bool ShortCircuited,
    IReadOnlyList<string> ExecutedStageNames);

public sealed class DefaultGenerationPipelineRunner : IGenerationPipelineRunner
{
    private readonly IReadOnlyList<IGenerationStage> _stages;
    private readonly ILogger<DefaultGenerationPipelineRunner> _logger;

    public DefaultGenerationPipelineRunner(
        IEnumerable<IGenerationStage> stages,
        ILogger<DefaultGenerationPipelineRunner> logger)
    {
        _stages = stages
            .Where(s => s.Order <= 120)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
        _logger = logger;
    }

    public async Task<PipelineRunOutcome> RunAsync(GenerationContext context, CancellationToken ct)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var executed = new List<string>(_stages.Count);

        foreach (var stage in _stages)
        {
            ct.ThrowIfCancellationRequested();

            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["stage"] = stage.Name,
                ["run_id"] = context.Orchestrator.Id
            });

            StageOutcome outcome;
            try
            {
                _logger.LogDebug("[Pipeline] Stage {Stage} starting", stage.Name);
                outcome = await stage.ExecuteAsync(context, ct).ConfigureAwait(false);
                executed.Add(stage.Name);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[Pipeline] Stage {Stage} cancelled", stage.Name);
                throw;
            }
            catch (Exception ex)
            {
                var reason = $"stage_exception:{stage.Name}:{ex.GetType().Name}:{ex.Message}";
                _logger.LogError(ex, "[Pipeline] Stage {Stage} threw; halting pipeline.", stage.Name);
                context.FailureReason ??= reason;
                executed.Add(stage.Name);
                return new PipelineRunOutcome(
                    Succeeded: false,
                    FailureReason: reason,
                    FailedStageName: stage.Name,
                    ShortCircuited: false,
                    ExecutedStageNames: executed);
            }

            if (outcome.ShortCircuit)
            {
                _logger.LogInformation("[Pipeline] Stage {Stage} short-circuited successfully.", stage.Name);
                return new PipelineRunOutcome(
                    Succeeded: true,
                    FailureReason: null,
                    FailedStageName: null,
                    ShortCircuited: true,
                    ExecutedStageNames: executed);
            }

            if (!outcome.ShouldContinue)
            {
                var reason = outcome.FailureReason ?? $"stage_failed:{stage.Name}";
                context.FailureReason ??= reason;
                _logger.LogWarning("[Pipeline] Stage {Stage} stopped pipeline: {Reason}", stage.Name, reason);
                return new PipelineRunOutcome(
                    Succeeded: false,
                    FailureReason: reason,
                    FailedStageName: stage.Name,
                    ShortCircuited: false,
                    ExecutedStageNames: executed);
            }
        }

        return new PipelineRunOutcome(
            Succeeded: true,
            FailureReason: null,
            FailedStageName: null,
            ShortCircuited: false,
            ExecutedStageNames: executed);
    }
}
