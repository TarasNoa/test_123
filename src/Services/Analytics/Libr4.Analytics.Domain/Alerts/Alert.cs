using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Analytics.Domain.Alerts.Events;

namespace Libr4.Analytics.Domain.Alerts;

public class Alert : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Condition { get; private set; } = string.Empty; // e.g., "cpu > 80"
    public AlertStatus Status { get; private set; }
    public Guid MetricId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Alert() { }

    public static Alert Create(string name, string condition, Guid metricId)
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            Name = name,
            Condition = condition,
            Status = AlertStatus.Inactive,
            MetricId = metricId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        alert.RaiseDomainEvent(new AlertCreatedEvent(alert.Id, name, condition, metricId));
        return alert;
    }

    public void Trigger()
    {
        if (Status != AlertStatus.Active)
        {
            Status = AlertStatus.Active;
            RaiseDomainEvent(new AlertTriggeredEvent(Id, Name, DateTimeOffset.UtcNow));
        }
    }

    public void Resolve()
    {
        if (Status == AlertStatus.Active)
        {
            Status = AlertStatus.Resolved;
            RaiseDomainEvent(new AlertResolvedEvent(Id, Name, DateTimeOffset.UtcNow));
        }
    }
}

public enum AlertStatus
{
    Inactive,
    Active,
    Resolved
}