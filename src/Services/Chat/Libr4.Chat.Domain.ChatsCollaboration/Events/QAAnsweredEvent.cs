using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record QAAnsweredEvent(Guid QAId, Guid ProjectId, Guid? UserId, Guid AnsweredBy, DateTimeOffset AnsweredAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
