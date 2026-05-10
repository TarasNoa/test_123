using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Analytics.Domain.Metrics.Events;

public record MetricCreatedEvent(Guid MetricId, string MetricName, double Value, DateTimeOffset Timestamp) : DomainEvent;