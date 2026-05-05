namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record CascadePlanAuditEntry(
    Guid RunId,
    string Rationale,
    string SerializedPlanJson,
    int PhaseCount,
    string RoutingProfile,
    string? ModelHint,
    string PlannerMode,
    DateTime CreatedAtUtc);
