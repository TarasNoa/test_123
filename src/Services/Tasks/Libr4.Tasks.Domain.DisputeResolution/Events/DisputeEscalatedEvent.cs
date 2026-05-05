using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.DisputeResolution.Events;

public record DisputeEscalatedEvent(Guid DisputeId, Guid TaskId, string Reason, DateTimeOffset EscalatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
