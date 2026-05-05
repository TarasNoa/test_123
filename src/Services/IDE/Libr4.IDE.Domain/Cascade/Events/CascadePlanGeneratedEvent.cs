using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.Cascade.Events;

/// <summary>
/// Domain event raised when a cascade plan is successfully generated
/// </summary>
public class CascadePlanGeneratedEvent : IDomainEvent
{
    public Guid OrchestratorPlanId { get; }
    public string PlanId { get; }
    public DateTime OccurredOn { get; }
    
    public CascadePlanGeneratedEvent(
        Guid orchestratorPlanId,
        string planId)
    {
        OrchestratorPlanId = orchestratorPlanId;
        PlanId = planId;
        OccurredOn = DateTime.UtcNow;
    }
}
