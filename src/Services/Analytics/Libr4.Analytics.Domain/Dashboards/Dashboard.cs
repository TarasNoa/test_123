using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Analytics.Domain.Dashboards.Events;

namespace Libr4.Analytics.Domain.Dashboards;

public enum DashboardType
{
    User,
    Admin,
    Project,
    Team
}

public enum WidgetType
{
    Chart,
    Metric,
    Table,
    Map,
    Gauge,
    Heatmap
}

public enum ReportFormat
{
    Json,
    Csv,
    Pdf,
    Email
}

public class Dashboard : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public List<DashboardWidget> Widgets { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    private Dashboard() { }

    public static Dashboard Create(string title, string description, Guid ownerId)
    {
        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dashboard.RaiseDomainEvent(new DashboardCreatedEvent(dashboard.Id, title, ownerId, dashboard.CreatedAt));
        return dashboard;
    }

    public void AddWidget(string widgetType, string config)
    {
        var widget = new DashboardWidget(Guid.NewGuid(), widgetType, config);
        Widgets.Add(widget);
        RaiseDomainEvent(new WidgetAddedEvent(Id, widget.Id, widgetType));
    }

    public void RemoveWidget(Guid widgetId)
    {
        var widget = Widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget != null)
        {
            Widgets.Remove(widget);
            RaiseDomainEvent(new WidgetRemovedEvent(Id, widgetId));
        }
    }
}

public record DashboardWidget(Guid Id, string Type, string Config);
