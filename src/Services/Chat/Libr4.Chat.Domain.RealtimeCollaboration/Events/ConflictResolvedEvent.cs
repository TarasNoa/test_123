using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.RealtimeCollaboration.Events;

public record ConflictResolvedEvent(Guid DocumentId, string DocumentName, ConflictResolution Resolution, Guid UserId, DateTimeOffset ResolvedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
