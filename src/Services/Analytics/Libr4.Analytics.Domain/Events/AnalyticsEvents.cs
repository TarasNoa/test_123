using Libr4.Shared.Kernel.Domain;

namespace Libr4.Analytics.Domain.Events;

public record AlertCreatedEvent(Guid AlertId, string Name, string Condition, Guid MetricId) : DomainEvent;
public record AlertTriggeredEvent(Guid AlertId, string Name, DateTimeOffset TriggeredAt) : DomainEvent;
public record AlertResolvedEvent(Guid AlertId, string Name, DateTimeOffset ResolvedAt) : DomainEvent;
public record MetricCreatedEvent(Guid MetricId, string Name, double Value, DateTimeOffset Timestamp) : DomainEvent;
public record MetricUpdatedEvent(Guid MetricId, string Name, double Value, DateTimeOffset Timestamp) : DomainEvent;

public record DashboardCreatedEvent(Guid DashboardId, string Title, Guid OwnerId, DateTimeOffset CreatedAt) : DomainEvent;
public record WidgetAddedEvent(Guid DashboardId, Guid WidgetId, string WidgetType) : DomainEvent;
public record WidgetRemovedEvent(Guid DashboardId, Guid WidgetId) : DomainEvent;
