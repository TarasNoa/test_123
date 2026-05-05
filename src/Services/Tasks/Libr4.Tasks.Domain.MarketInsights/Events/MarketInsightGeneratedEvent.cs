using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.MarketInsights.Events;

public record MarketInsightGeneratedEvent(Guid InsightId, string Category, string InsightType, DateTimeOffset GeneratedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
