using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Agents.Events;

public record AgentActivatedEvent(Guid AgentId, DateTime ActivatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
