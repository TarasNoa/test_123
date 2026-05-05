using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AutonomousRuntimePolicy.Events;

/// <summary>
/// Domain event raised when a quality contract is required
/// </summary>
public class QualityContractRequiredEvent : IDomainEvent
{
    public Guid RuntimePolicyId { get; }
    public string PolicyId { get; }
    public string ApprovalWorkflow { get; }
    public DateTime OccurredOn { get; }
    
    public QualityContractRequiredEvent(
        Guid runtimePolicyId,
        string policyId,
        string approvalWorkflow)
    {
        RuntimePolicyId = runtimePolicyId;
        PolicyId = policyId;
        ApprovalWorkflow = approvalWorkflow;
        OccurredOn = DateTime.UtcNow;
    }
}
