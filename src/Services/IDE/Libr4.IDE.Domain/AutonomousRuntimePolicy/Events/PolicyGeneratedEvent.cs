using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AutonomousRuntimePolicy.Events;

/// <summary>
/// Domain event raised when a runtime policy is generated
/// </summary>
public class PolicyGeneratedEvent : IDomainEvent
{
    public Guid RuntimePolicyId { get; }
    public string PolicyId { get; }
    public DomainSignal DomainSignal { get; }
    public DateTime OccurredOn { get; }
    
    public PolicyGeneratedEvent(
        Guid runtimePolicyId,
        string policyId,
        DomainSignal domainSignal)
    {
        RuntimePolicyId = runtimePolicyId;
        PolicyId = policyId;
        DomainSignal = domainSignal;
        OccurredOn = DateTime.UtcNow;
    }
}
