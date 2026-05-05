using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.OrchestrationRun.Events;

/// <summary>
/// Domain event raised when a workflow transition occurs
/// </summary>
public class WorkflowTransitionEvent : IDomainEvent
{
    public Guid OrchestrationRunId { get; }
    public string RunId { get; }
    public string FromState { get; }
    public string ToState { get; }
    public DateTime OccurredOn { get; }
    
    public WorkflowTransitionEvent(
        Guid orchestrationRunId,
        string runId,
        string fromState,
        string toState)
    {
        OrchestrationRunId = orchestrationRunId;
        RunId = runId;
        FromState = fromState;
        ToState = toState;
        OccurredOn = DateTime.UtcNow;
    }
}
