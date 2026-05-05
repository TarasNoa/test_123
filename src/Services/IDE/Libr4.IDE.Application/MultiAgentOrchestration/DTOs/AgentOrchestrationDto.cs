namespace Libr4.IDE.Application.MultiAgentOrchestration.DTOs;

/// <summary>
/// DTO for AgentOrchestration
/// </summary>
public record AgentOrchestrationDto
{
    public Guid Id { get; init; }
    public string OrchestrationId { get; init; } = string.Empty;
    public List<AgentInstanceDto> Agents { get; init; } = new();
    public OrchestrationTaskDto MainTask { get; init; } = null!;
    public List<AgentCommunicationDto> Communications { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// DTO for AgentCommunication
/// </summary>
public record AgentCommunicationDto
{
    public Guid Id { get; init; }
    public Guid FromAgentId { get; init; }
    public Guid ToAgentId { get; init; }
    public string Message { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}
