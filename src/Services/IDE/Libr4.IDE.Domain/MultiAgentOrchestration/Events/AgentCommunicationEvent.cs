using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.MultiAgentOrchestration.Events;

/// <summary>
/// Domain event raised when agents communicate
/// </summary>
public class AgentCommunicationEvent : IDomainEvent
{
    public Guid AgentOrchestrationId { get; }
    public string OrchestrationId { get; }
    public Guid FromAgentId { get; }
    public Guid ToAgentId { get; }
    public DateTime OccurredOn { get; }
    
    public AgentCommunicationEvent(
        Guid agentOrchestrationId,
        string orchestrationId,
        Guid fromAgentId,
        Guid toAgentId)
    {
        AgentOrchestrationId = agentOrchestrationId;
        OrchestrationId = orchestrationId;
        FromAgentId = fromAgentId;
        ToAgentId = toAgentId;
        OccurredOn = DateTime.UtcNow;
    }
}
