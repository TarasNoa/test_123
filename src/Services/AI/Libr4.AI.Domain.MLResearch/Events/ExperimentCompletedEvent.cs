using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.MLResearch.Events;

public record ExperimentCompletedEvent(Guid ExperimentId, string Title, float Accuracy, float Loss, DateTimeOffset CompletedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
