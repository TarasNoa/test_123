using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.SmartAssistant.Events;

public record AssistantSessionEndedEvent(Guid SessionId, Guid UserId, string SessionType, DateTimeOffset EndedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
