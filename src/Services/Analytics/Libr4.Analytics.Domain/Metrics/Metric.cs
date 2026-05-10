using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Analytics.Domain.Metrics.Events;

namespace Libr4.Analytics.Domain.Metrics;

public class Metric : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // e.g., "counter", "gauge", "histogram"
    public double Value { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public Dictionary<string, string> Labels { get; private set; } = new();

    private Metric() { }

    public static Metric Create(string name, string type, double value, Dictionary<string, string> labels)
    {
        var metric = new Metric
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Value = value,
            Timestamp = DateTimeOffset.UtcNow,
            Labels = labels ?? new Dictionary<string, string>()
        };

        metric.RaiseDomainEvent(new MetricCreatedEvent(metric.Id, name, value, metric.Timestamp));
        return metric;
    }

    public void UpdateValue(double newValue)
    {
        Value = newValue;
        Timestamp = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new MetricUpdatedEvent(Id, Name, newValue, Timestamp));
    }
}