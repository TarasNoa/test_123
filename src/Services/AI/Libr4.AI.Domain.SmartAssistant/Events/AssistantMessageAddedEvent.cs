using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.SmartAssistant.Events;

public record AssistantMessageAddedEvent(Guid SessionId, Guid UserId, string Role, DateTimeOffset AddedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
