using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.ArchitecturalGuardrails.Events;

/// <summary>
/// Domain event raised when validation is completed
/// </summary>
public class ValidationCompletedEvent : IDomainEvent
{
    public Guid ArchitectureValidationId { get; }
    public string ValidationId { get; }
    public int ViolationsCount { get; }
    public DateTime OccurredOn { get; }
    
    public ValidationCompletedEvent(
        Guid architectureValidationId,
        string validationId,
        int violationsCount)
    {
        ArchitectureValidationId = architectureValidationId;
        ValidationId = validationId;
        ViolationsCount = violationsCount;
        OccurredOn = DateTime.UtcNow;
    }
}
