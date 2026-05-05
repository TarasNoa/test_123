namespace Libr4.IDE.Application.LLMRouter.DTOs;

/// <summary>
/// DTO for LLMRouting
/// </summary>
public record LLMRoutingDto
{
    public Guid Id { get; init; }
    public string RoutingId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public int EstimatedTokens { get; init; }
    public RoutingDecisionDto? Decision { get; init; }
    public double CostSavings { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for RoutingDecision
/// </summary>
public record RoutingDecisionDto
{
    public LLMModelDto SelectedModel { get; init; } = null!;
    public double EstimatedCost { get; init; }
    public double EstimatedLatency { get; init; }
    public string Rationale { get; init; } = string.Empty;
}
