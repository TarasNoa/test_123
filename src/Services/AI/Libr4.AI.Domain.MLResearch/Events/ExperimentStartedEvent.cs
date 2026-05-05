using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.MLResearch.Events;

public record ExperimentStartedEvent(Guid ExperimentId, string Title, ResearchArea Area, DateTimeOffset StartedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
