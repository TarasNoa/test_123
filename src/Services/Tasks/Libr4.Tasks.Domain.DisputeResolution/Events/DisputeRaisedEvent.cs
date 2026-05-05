using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.DisputeResolution.Events;

public record DisputeRaisedEvent(Guid DisputeId, Guid TaskId, Guid RaisedBy, string DisputeType, DateTimeOffset RaisedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
