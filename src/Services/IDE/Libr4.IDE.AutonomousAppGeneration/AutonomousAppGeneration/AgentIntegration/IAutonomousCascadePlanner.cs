using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed record CascadeExecutionPhase(
    string PhaseId,
    string PhaseName,
    string Description,
    IReadOnlyList<string> Dependencies,
    string ExpectedOutput,
    IReadOnlyDictionary<string, string> Instructions);

public sealed record CascadeExecutionPlan(
    IReadOnlyList<CascadeExecutionPhase> Phases,
    string Rationale,
    string OrchestratorJson,
    string RoutingProfile = "deterministic",
    string? ModelHint = null,
    string PlannerMode = "deterministic");

public interface IAutonomousCascadePlanner
{
    CascadeExecutionPlan Build(GenerationPlan plan, string userRequest);
}

