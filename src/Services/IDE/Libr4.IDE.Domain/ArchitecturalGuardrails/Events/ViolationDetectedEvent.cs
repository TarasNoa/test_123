using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.ArchitecturalGuardrails.Events;

/// <summary>
/// Domain event raised when a violation is detected
/// </summary>
public class ViolationDetectedEvent : IDomainEvent
{
    public Guid ArchitectureValidationId { get; }
    public string ValidationId { get; }
    public string RuleName { get; }
    public DateTime OccurredOn { get; }
    
    public ViolationDetectedEvent(
        Guid architectureValidationId,
        string validationId,
        string ruleName)
    {
        ArchitectureValidationId = architectureValidationId;
        ValidationId = validationId;
        RuleName = ruleName;
        OccurredOn = DateTime.UtcNow;
    }
}
