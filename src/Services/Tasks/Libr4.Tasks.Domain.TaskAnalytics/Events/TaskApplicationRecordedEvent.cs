using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskAnalytics.Events;

public record TaskApplicationRecordedEvent(Guid AnalyticsId, Guid TaskId, int TotalApplications, DateTimeOffset RecordedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
