using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.OrchestrationRun.Events;

/// <summary>
/// Domain event raised when an orchestration run is started
/// </summary>
public class OrchestrationRunStartedEvent : IDomainEvent
{
    public Guid OrchestrationRunId { get; }
    public string RunId { get; }
    public string TaskId { get; }
    public DateTime OccurredOn { get; }
    
    public OrchestrationRunStartedEvent(
        Guid orchestrationRunId,
        string runId,
        string taskId)
    {
        OrchestrationRunId = orchestrationRunId;
        RunId = runId;
        TaskId = taskId;
        OccurredOn = DateTime.UtcNow;
    }
}
