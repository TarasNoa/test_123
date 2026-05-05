using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record ChatMessageDeletedEvent(Guid MessageId, Guid ChatId, Guid UserId, DateTimeOffset DeletedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
