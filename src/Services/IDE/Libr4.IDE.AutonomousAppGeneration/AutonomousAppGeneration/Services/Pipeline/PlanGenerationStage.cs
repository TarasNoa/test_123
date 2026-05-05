using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 stage: invokes <see cref="IAppPlannerService"/> if no plan is set on the
/// context. Skips when a resume seed plan is already attached (resume path).
/// Caps the plan's <c>MaxIterations</c> by <see cref="GenerationContext.RequestedMaxIterations"/>
/// when the latter is tighter.
///
/// Order=100 — first stage that produces a plan.
/// </summary>
public sealed class PlanGenerationStage : IGenerationStage
{
    private readonly IAppPlannerService _planner;
    private readonly ILogger<PlanGenerationStage> _logger;

    public PlanGenerationStage(
        IAppPlannerService planner,
        ILogger<PlanGenerationStage> logger)
    {
        _planner = planner;
        _logger = logger;
    }

    public string Name => "plan_generation";
    public int Order => 100;

    public async Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Plan is not null)
        {
            // Resume path: plan was pre-populated by caller.
            _logger.LogDebug("[Pipeline] Plan already present; skipping plan generation.");
            return StageOutcome.Continue;
        }

        try
        {
            var plan = await _planner.PlanAsync(context.UserRequest, ct).ConfigureAwait(false);
            context.Plan = ApplyMaxIterationsCap(plan, context.RequestedMaxIterations);
            return StageOutcome.Continue;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pipeline] Planner failed for run {RunId}", context.Orchestrator.Id);
            return StageOutcome.Stop($"plan_generation_failed:{ex.GetType().Name}:{ex.Message}");
        }
    }

    private static GenerationPlan ApplyMaxIterationsCap(GenerationPlan plan, int requested)
    {
        if (requested <= 0 || requested >= plan.MaxIterations)
            return plan;

        return new GenerationPlan(
            plan.ApplicationName,
            plan.ApplicationDescription,
            plan.TechStack,
            plan.Phases,
            plan.RequiredAgents,
            plan.RuntimeImage,
            plan.BuildCommands,
            plan.TestCommands,
            requested);
    }
}
