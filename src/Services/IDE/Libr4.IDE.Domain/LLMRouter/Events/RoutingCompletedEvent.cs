using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.LLMRouter.Events;

/// <summary>
/// Domain event raised when LLM routing is completed
/// </summary>
public class RoutingCompletedEvent : IDomainEvent
{
    public Guid LLMRoutingId { get; }
    public string RoutingId { get; }
    public DateTime OccurredOn { get; }
    
    public RoutingCompletedEvent(
        Guid llmRoutingId,
        string routingId)
    {
        LLMRoutingId = llmRoutingId;
        RoutingId = routingId;
        OccurredOn = DateTime.UtcNow;
    }
}
