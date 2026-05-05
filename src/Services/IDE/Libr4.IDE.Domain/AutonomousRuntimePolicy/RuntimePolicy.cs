using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.AutonomousRuntimePolicy.Events;

namespace Libr4.IDE.Domain.AutonomousRuntimePolicy;

/// <summary>
/// AggregateRoot for runtime policy
/// </summary>
public class RuntimePolicy : AggregateRoot<Guid>
{
    public string PolicyId { get; private set; }
    public string Prompt { get; private set; }
    public string WorkspaceId { get; private set; }
    public DomainSignal DomainSignal { get; private set; }
    public RuntimeEvidenceSignal RuntimeEvidenceSignal { get; private set; }
    public bool RuntimeProofRequired { get; private set; }
    public bool RichAppBuildRequired { get; private set; }
    public QualityContract QualityContract { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private RuntimePolicy() { }
    
    public RuntimePolicy(
        string policyId,
        string prompt,
        string workspaceId,
        DomainSignal domainSignal,
        RuntimeEvidenceSignal runtimeEvidenceSignal,
        bool runtimeProofRequired,
        bool richAppBuildRequired,
        QualityContract qualityContract)
    {
        Id = Guid.NewGuid();
        PolicyId = policyId;
        Prompt = prompt;
        WorkspaceId = workspaceId;
        DomainSignal = domainSignal;
        RuntimeEvidenceSignal = runtimeEvidenceSignal;
        RuntimeProofRequired = runtimeProofRequired;
        RichAppBuildRequired = richAppBuildRequired;
        QualityContract = qualityContract;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetQualityContract(QualityContract qualityContract)
    {
        QualityContract = qualityContract;
    }
    
    /// <summary>
    /// Marks the policy as generated and raises a domain event
    /// </summary>
    public void MarkAsGenerated()
    {
        AddDomainEvent(new PolicyGeneratedEvent(Id, PolicyId, DomainSignal));
    }
    
    /// <summary>
    /// Marks that a quality contract is required and raises a domain event
    /// </summary>
    public void MarkQualityContractRequired()
    {
        AddDomainEvent(new QualityContractRequiredEvent(Id, PolicyId, QualityContract.ApprovalWorkflow));
    }
    
    public static RuntimePolicy Create(
        string policyId,
        string prompt,
        string workspaceId,
        DomainSignal domainSignal,
        RuntimeEvidenceSignal runtimeEvidenceSignal,
        bool runtimeProofRequired,
        bool richAppBuildRequired,
        QualityContract qualityContract)
    {
        return new RuntimePolicy(policyId, prompt, workspaceId, domainSignal, runtimeEvidenceSignal, runtimeProofRequired, richAppBuildRequired, qualityContract);
    }
}
