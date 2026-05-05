namespace Libr4.IDE.Application.AIWorkflowAutomation.DTOs;

/// <summary>
/// DTO for WorkflowPattern
/// </summary>
public record WorkflowPatternDto
{
    public Guid Id { get; init; }
    public string PatternName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Steps { get; init; } = new();
    public int Frequency { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
