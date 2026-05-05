namespace Libr4.IDE.Application.MultiAgentOrchestration.DTOs;

/// <summary>
/// DTO for AgentInstance
/// </summary>
public record AgentInstanceDto
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double PerformanceScore { get; init; }
    public List<string> Capabilities { get; init; } = new();
    public Dictionary<string, object> SpecializationProfile { get; init; } = new();
}
