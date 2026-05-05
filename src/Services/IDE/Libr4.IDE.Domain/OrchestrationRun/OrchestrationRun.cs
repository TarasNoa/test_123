using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.OrchestrationRun.Events;

namespace Libr4.IDE.Domain.OrchestrationRun;

/// <summary>
/// AggregateRoot representing an orchestration run
/// </summary>
public class OrchestrationRun : AggregateRoot<Guid>
{
    public string RunId { get; private set; }
    public string TaskId { get; private set; }
    public string CurrentState { get; private set; }
    public Skill SelectedSkill { get; private set; }
    public List<WorkflowTransition> Transitions { get; private set; }
    public Dictionary<string, object> HookMilestones { get; private set; }
    public string Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private OrchestrationRun() { }
    
    public OrchestrationRun(
        string runId,
        string taskId,
        Skill selectedSkill,
        string initialState = "idle")
    {
        Id = Guid.NewGuid();
        RunId = runId;
        TaskId = taskId;
        CurrentState = initialState;
        SelectedSkill = selectedSkill;
        Transitions = new List<WorkflowTransition>();
        HookMilestones = new Dictionary<string, object>();
        Status = "running";
        StartedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddTransition(WorkflowTransition transition)
    {
        if (transition != null)
        {
            Transitions.Add(transition);
            CurrentState = transition.ToState;
        }
    }
    
    public void SetHookMilestone(string milestone, object value)
    {
        if (!string.IsNullOrWhiteSpace(milestone))
        {
            HookMilestones[milestone] = value;
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    public void SelectSkill(Skill skill)
    {
        SelectedSkill = skill;
    }
    
    /// <summary>
    /// Marks the orchestration run as started and raises a domain event
    /// </summary>
    public void MarkAsStarted()
    {
        AddDomainEvent(new OrchestrationRunStartedEvent(Id, RunId, TaskId));
    }
    
    /// <summary>
    /// Marks a workflow transition and raises a domain event
    /// </summary>
    public void MarkTransition(WorkflowTransition transition)
    {
        AddDomainEvent(new WorkflowTransitionEvent(Id, RunId, transition.FromState, transition.ToState));
    }
    
    public static OrchestrationRun Create(
        string runId,
        string taskId,
        Skill selectedSkill,
        string initialState = "idle")
    {
        return new OrchestrationRun(runId, taskId, selectedSkill, initialState);
    }
}
