using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.PlanAgent;

public interface IPlanAgentService
{
    Task<GenerationPlan> BuildPlanAsync(string userRequest, CancellationToken ct = default);
}

public sealed class PlanAgentService : IPlanAgentService
{
    private readonly IAppPlannerService _planner;

    public PlanAgentService(IAppPlannerService planner)
    {
        _planner = planner;
    }

    public Task<GenerationPlan> BuildPlanAsync(string userRequest, CancellationToken ct = default)
    {
        // Priority 3 extension layer: dedicated plan-agent entrypoint that can
        // be enriched independently from the base planner contract.
        var enrichedRequest = $"{userRequest}\n\n[plan-agent]: enforce deterministic phased execution and deployment-ready artifacts.";
        return _planner.PlanAsync(enrichedRequest, ct);
    }
}
