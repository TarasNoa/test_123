namespace Libr4.IDE.Application.IntelligenceRouter.DTOs;

/// <summary>
/// DTO for RoutingDecision
/// </summary>
public record RoutingDecisionDto
{
    public string PhaseId { get; init; } = string.Empty;
    public string PhaseName { get; init; } = string.Empty;
    public string Complexity { get; init; } = string.Empty;
    public string SelectedProvider { get; init; } = string.Empty;
    public string SelectedModel { get; init; } = string.Empty;
    public List<string> SelectedTools { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public Dictionary<string, object> ContextQueries { get; init; } = new();
}
