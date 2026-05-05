using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskAnalytics.Events;

public record TaskViewedEvent(Guid AnalyticsId, Guid TaskId, int TotalViews, DateTimeOffset ViewedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
