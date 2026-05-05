namespace Libr4.IDE.Application.IntelligenceRouter.DTOs;

/// <summary>
/// DTO for RoutingPlan
/// </summary>
public record RoutingPlanDto
{
    public Guid Id { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public List<RoutingDecisionDto> PhaseDecisions { get; init; } = new();
    public string PrimaryProvider { get; init; } = string.Empty;
    public string PrimaryModel { get; init; } = string.Empty;
    public List<string> GlobalTools { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
}
