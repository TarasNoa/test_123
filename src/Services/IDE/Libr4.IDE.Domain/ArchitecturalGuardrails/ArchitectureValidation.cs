using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.ArchitecturalGuardrails.Events;

namespace Libr4.IDE.Domain.ArchitecturalGuardrails;

/// <summary>
/// AggregateRoot for architecture validation
/// </summary>
public class ArchitectureValidation : AggregateRoot<Guid>
{
    public string ValidationId { get; private set; }
    public string WorkspaceId { get; private set; }
    public List<GuardrailRule> Rules { get; private set; }
    public List<GuardrailViolation> Violations { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private ArchitectureValidation() { }
    
    public ArchitectureValidation(
        string validationId,
        string workspaceId,
        List<GuardrailRule>? rules = null,
        List<GuardrailViolation>? violations = null)
    {
        Id = Guid.NewGuid();
        ValidationId = validationId;
        WorkspaceId = workspaceId;
        Rules = rules ?? new List<GuardrailRule>();
        Violations = violations ?? new List<GuardrailViolation>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddRule(GuardrailRule rule)
    {
        if (rule != null)
        {
            Rules.Add(rule);
        }
    }
    
    public void AddViolation(GuardrailViolation violation)
    {
        if (violation != null)
        {
            Violations.Add(violation);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Marks the validation as started and raises a domain event
    /// </summary>
    public void MarkAsStarted()
    {
        AddDomainEvent(new ValidationStartedEvent(Id, ValidationId));
    }
    
    /// <summary>
    /// Marks a violation as detected and raises a domain event
    /// </summary>
    public void MarkViolationDetected(GuardrailViolation violation)
    {
        AddDomainEvent(new ViolationDetectedEvent(Id, ValidationId, violation.Rule.RuleName));
    }
    
    /// <summary>
    /// Marks the validation as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new ValidationCompletedEvent(Id, ValidationId, Violations.Count));
    }
    
    public static ArchitectureValidation Create(
        string validationId,
        string workspaceId,
        List<GuardrailRule>? rules = null,
        List<GuardrailViolation>? violations = null)
    {
        return new ArchitectureValidation(validationId, workspaceId, rules, violations);
    }
}
