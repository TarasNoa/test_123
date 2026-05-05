using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record ChatMessageEditedEvent(Guid MessageId, Guid ChatId, Guid UserId, DateTimeOffset EditedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
