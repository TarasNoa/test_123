namespace Libr4.IDE.Application.MultiAgentOrchestration.DTOs;

/// <summary>
/// DTO for OrchestrationTask
/// </summary>
public record OrchestrationTaskDto
{
    public Guid Id { get; init; }
    public string TaskId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<OrchestrationTaskDto> Subtasks { get; init; } = new();
    public List<string> Dependencies { get; init; } = new();
    public Guid? AssignedAgentId { get; init; }
    public string Status { get; init; } = string.Empty;
}
