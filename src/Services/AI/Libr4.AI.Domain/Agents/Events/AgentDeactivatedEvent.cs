using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Agents.Events;

public record AgentDeactivatedEvent(Guid AgentId, DateTime DeactivatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
