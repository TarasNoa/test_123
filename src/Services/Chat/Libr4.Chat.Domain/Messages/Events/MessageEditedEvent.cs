using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Messages.Events;

public record MessageEditedEvent(Guid MessageId, Guid ChatId, Guid SenderId, DateTime EditedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
