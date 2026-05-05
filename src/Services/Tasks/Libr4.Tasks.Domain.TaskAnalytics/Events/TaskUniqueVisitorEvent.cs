using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskAnalytics.Events;

public record TaskUniqueVisitorEvent(Guid AnalyticsId, Guid TaskId, int UniqueVisitors, DateTimeOffset VisitedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
