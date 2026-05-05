using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Agents.Events;

public record AgentCreatedEvent(Guid AgentId, string Name, AgentType Type, string Model, DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
