using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.LLMRouter.Events;

/// <summary>
/// Domain event raised when cost is optimized
/// </summary>
public class CostOptimizedEvent : IDomainEvent
{
    public Guid LLMRoutingId { get; }
    public string RoutingId { get; }
    public double Savings { get; }
    public DateTime OccurredOn { get; }
    
    public CostOptimizedEvent(
        Guid llmRoutingId,
        string routingId,
        double savings)
    {
        LLMRoutingId = llmRoutingId;
        RoutingId = routingId;
        Savings = savings;
        OccurredOn = DateTime.UtcNow;
    }
}
