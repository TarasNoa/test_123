namespace Libr4.IDE.Domain.Common.Events;

/// <summary>
/// Interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
