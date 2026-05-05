using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.MLResearch.Events;

public record ExperimentFailedEvent(Guid ExperimentId, string Title, string Reason, DateTimeOffset FailedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
