namespace Libr4.IDE.Application.AIWorkflowAutomation.DTOs;

/// <summary>
/// DTO for WorkflowAnalysis
/// </summary>
public record WorkflowAnalysisDto
{
    public Guid Id { get; init; }
    public string AnalysisId { get; init; } = string.Empty;
    public string WorkflowId { get; init; } = string.Empty;
    public List<WorkflowPatternDto> Patterns { get; init; } = new();
    public List<ExtractedSkillDto> ExtractedSkills { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
