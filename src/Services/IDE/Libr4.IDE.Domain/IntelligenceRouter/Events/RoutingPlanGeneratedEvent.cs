using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.IntelligenceRouter.Events;

/// <summary>
/// Domain event raised when a routing plan is successfully generated
/// </summary>
public class RoutingPlanGeneratedEvent : IDomainEvent
{
    public Guid RoutingPlanId { get; }
    public string PlanId { get; }
    public string PrimaryProvider { get; }
    public DateTime OccurredOn { get; }
    
    public RoutingPlanGeneratedEvent(
        Guid routingPlanId,
        string planId,
        string primaryProvider)
    {
        RoutingPlanId = routingPlanId;
        PlanId = planId;
        PrimaryProvider = primaryProvider;
        OccurredOn = DateTime.UtcNow;
    }
}
