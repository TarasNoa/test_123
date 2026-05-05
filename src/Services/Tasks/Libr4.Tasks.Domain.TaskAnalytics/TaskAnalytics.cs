using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.TaskAnalytics.Events;

namespace Libr4.Tasks.Domain.TaskAnalytics;

public class TaskAnalytics : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public int TotalViews { get; private set; }
    public int TotalApplications { get; private set; }
    public int UniqueVisitors { get; private set; }
    public float ConversionRate { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }

    private TaskAnalytics() { }

    public void RecordView(DateTimeOffset now)
    {
        TotalViews++;
        RecalculateMetrics(now);
        RaiseDomainEvent(new TaskViewedEvent(Id, TaskId, TotalViews, now));
    }

    public void RecordApplication(DateTimeOffset now)
    {
        TotalApplications++;
        RecalculateMetrics(now);
        RaiseDomainEvent(new TaskApplicationRecordedEvent(Id, TaskId, TotalApplications, now));
    }

    public void RecordUniqueVisitor(DateTimeOffset now)
    {
        UniqueVisitors++;
        RecalculateMetrics(now);
        RaiseDomainEvent(new TaskUniqueVisitorEvent(Id, TaskId, UniqueVisitors, now));
    }

    private void RecalculateMetrics(DateTimeOffset now)
    {
        ConversionRate = TotalViews > 0 ? (float)TotalApplications / TotalViews * 100f : 0f;
        LastUpdated = now;
    }
}

public class TaskPerformanceMetrics
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public float AverageRating { get; set; }
    public int CompletionRate { get; set; }
    public int AverageCompletionTime { get; set; } // in days
    public float DisputeRate { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}
