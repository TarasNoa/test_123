using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.ArchitecturalGuardrails.Events;

/// <summary>
/// Domain event raised when validation is started
/// </summary>
public class ValidationStartedEvent : IDomainEvent
{
    public Guid ArchitectureValidationId { get; }
    public string ValidationId { get; }
    public DateTime OccurredOn { get; }
    
    public ValidationStartedEvent(
        Guid architectureValidationId,
        string validationId)
    {
        ArchitectureValidationId = architectureValidationId;
        ValidationId = validationId;
        OccurredOn = DateTime.UtcNow;
    }
}
