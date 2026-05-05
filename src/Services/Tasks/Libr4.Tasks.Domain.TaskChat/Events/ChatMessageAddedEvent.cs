using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskChat.Events;

public record ChatMessageAddedEvent(Guid ChatId, Guid TaskId, Guid SenderId, string Content, DateTimeOffset AddedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
