using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.MultiAgentOrchestration.Events;

/// <summary>
/// Domain event raised when an agent is assigned to a task
/// </summary>
public class AgentAssignedEvent : IDomainEvent
{
    public Guid AgentOrchestrationId { get; }
    public string OrchestrationId { get; }
    public Guid AgentId { get; }
    public Guid TaskId { get; }
    public DateTime OccurredOn { get; }
    
    public AgentAssignedEvent(
        Guid agentOrchestrationId,
        string orchestrationId,
        Guid agentId,
        Guid taskId)
    {
        AgentOrchestrationId = agentOrchestrationId;
        OrchestrationId = orchestrationId;
        AgentId = agentId;
        TaskId = taskId;
        OccurredOn = DateTime.UtcNow;
    }
}
