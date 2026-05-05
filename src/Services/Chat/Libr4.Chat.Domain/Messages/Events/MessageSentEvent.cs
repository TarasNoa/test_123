using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Messages.Events;

public record MessageSentEvent(Guid MessageId, Guid ChatId, Guid SenderId, MessageType Type, DateTime SentAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
