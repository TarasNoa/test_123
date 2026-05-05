namespace Libr4.IDE.Application.OrchestrationRun.DTOs;

/// <summary>
/// DTO for OrchestrationRun
/// </summary>
public record OrchestrationRunDto
{
    public Guid Id { get; init; }
    public string RunId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string CurrentState { get; init; } = string.Empty;
    public SkillDto SelectedSkill { get; init; } = null!;
    public List<WorkflowTransitionDto> Transitions { get; init; } = new();
    public Dictionary<string, object> HookMilestones { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// DTO for WorkflowTransition
/// </summary>
public record WorkflowTransitionDto
{
    public string FromState { get; init; } = string.Empty;
    public string ToState { get; init; } = string.Empty;
    public string TransitionType { get; init; } = string.Empty;
    public Dictionary<string, object> TransitionData { get; init; } = new();
    public DateTime TransitionedAt { get; init; }
}
