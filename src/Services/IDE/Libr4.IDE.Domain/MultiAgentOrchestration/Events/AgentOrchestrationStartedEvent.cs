using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.MultiAgentOrchestration.Events;

/// <summary>
/// Domain event raised when a multi-agent orchestration is started
/// </summary>
public class AgentOrchestrationStartedEvent : IDomainEvent
{
    public Guid AgentOrchestrationId { get; }
    public string OrchestrationId { get; }
    public DateTime OccurredOn { get; }
    
    public AgentOrchestrationStartedEvent(
        Guid agentOrchestrationId,
        string orchestrationId)
    {
        AgentOrchestrationId = agentOrchestrationId;
        OrchestrationId = orchestrationId;
        OccurredOn = DateTime.UtcNow;
    }
}
