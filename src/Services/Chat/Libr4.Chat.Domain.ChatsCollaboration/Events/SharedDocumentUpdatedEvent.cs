using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record SharedDocumentUpdatedEvent(Guid DocumentId, Guid ChatId, Guid OwnerId, int Version, DateTimeOffset UpdatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
