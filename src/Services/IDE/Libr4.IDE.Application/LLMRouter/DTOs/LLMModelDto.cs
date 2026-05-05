namespace Libr4.IDE.Application.LLMRouter.DTOs;

/// <summary>
/// DTO for LLMModel
/// </summary>
public record LLMModelDto
{
    public Guid Id { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public double CostPer1KTokens { get; init; }
    public int MaxTokens { get; init; }
    public double LatencyMs { get; init; }
    public Dictionary<string, object> Capabilities { get; init; } = new();
}
