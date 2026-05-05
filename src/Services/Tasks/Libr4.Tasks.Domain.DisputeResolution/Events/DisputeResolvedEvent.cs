using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.DisputeResolution.Events;

public record DisputeResolvedEvent(Guid DisputeId, Guid TaskId, string Resolution, DateTimeOffset ResolvedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
