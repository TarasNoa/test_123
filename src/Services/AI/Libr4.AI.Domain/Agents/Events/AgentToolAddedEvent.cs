using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Agents.Events;

public record AgentToolAddedEvent(Guid AgentId, string ToolName, DateTime AddedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
