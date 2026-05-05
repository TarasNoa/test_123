using Libr4.Shared.Kernel.Domain;

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
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DashboardType Type { get; private set; }
    public bool IsPublic { get; private set; }
    public int RefreshInterval { get; private set; } = 300; // Default 5 minutes
    
    // Permissions
    public List<string> Permissions { get; private set; } = new();
    
    // Filters
    public Dictionary<string, object> Filters { get; private set; } = new();
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<Widget> _widgets = new();
    public IReadOnlyCollection<Widget> Widgets => _widgets.AsReadOnly();

    private Dashboard() { }

    public static Dashboard Create(
        Guid userId,
        string name,
        string description,
        DashboardType type,
        bool isPublic = false,
        int refreshInterval = 300)
    {
        return new Dashboard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Type = type,
            IsPublic = isPublic,
            RefreshInterval = refreshInterval,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddWidget(Widget widget)
    {
        _widgets.Add(widget);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveWidget(Guid widgetId)
    {
        _widgets.RemoveAll(w => w.Id == widgetId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateWidget(Widget widget)
    {
        var existing = _widgets.FirstOrDefault(w => w.Id == widget.Id);
        if (existing != null)
        {
            _widgets.Remove(existing);
            _widgets.Add(widget);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void AddPermission(string permission)
    {
        if (!Permissions.Contains(permission))
        {
            Permissions.Add(permission);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetFilter(string key, object value)
    {
        Filters[key] = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePublic()
    {
        IsPublic = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePrivate()
    {
        IsPublic = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class Widget : Entity<Guid>
{
    public Guid DashboardId { get; private set; }
    public WidgetType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string DataSource { get; private set; } = string.Empty;
    public Dictionary<string, object> QueryConfig { get; private set; } = new();
    public Dictionary<string, object> VisualizationConfig { get; private set; } = new();
    public Dictionary<string, int> Position { get; private set; } = new(); // { x, y }
    public Dictionary<string, int> Size { get; private set; } = new(); // { width, height }

    private Widget() { }

    public static Widget Create(
        Guid dashboardId,
        WidgetType type,
        string title,
        string dataSource,
        Dictionary<string, object>? queryConfig = null,
        Dictionary<string, object>? visualizationConfig = null,
        Dictionary<string, int>? position = null,
        Dictionary<string, int>? size = null)
    {
        return new Widget
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboardId,
            Type = type,
            Title = title,
            DataSource = dataSource,
            QueryConfig = queryConfig ?? new Dictionary<string, object>(),
            VisualizationConfig = visualizationConfig ?? new Dictionary<string, object>(),
            Position = position ?? new Dictionary<string, int> { { "x", 0 }, { "y", 0 } },
            Size = size ?? new Dictionary<string, int> { { "width", 6 }, { "height", 4 } }
        };
    }

    public void UpdateQueryConfig(Dictionary<string, object> config)
    {
        QueryConfig = config;
    }

    public void UpdateVisualizationConfig(Dictionary<string, object> config)
    {
        VisualizationConfig = config;
    }

    public void SetPosition(int x, int y)
    {
        Position["x"] = x;
        Position["y"] = y;
    }

    public void SetSize(int width, int height)
    {
        Size["width"] = width;
        Size["height"] = height;
    }
}

public class CustomReport : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Dictionary<string, object> QueryDefinition { get; private set; } = new();
    public string? Schedule { get; private set; } // Cron expression
    public List<string> Recipients { get; private set; } = new();
    public ReportFormat Format { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }

    private CustomReport() { }

    public static CustomReport Create(
        Guid userId,
        string name,
        string description,
        Dictionary<string, object> queryDefinition,
        string? schedule = null,
        List<string>? recipients = null,
        ReportFormat format = ReportFormat.Json)
    {
        return new CustomReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            QueryDefinition = queryDefinition,
            Schedule = schedule,
            Recipients = recipients ?? new List<string>(),
            Format = format,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateSchedule(string schedule)
    {
        Schedule = schedule;
    }

    public void AddRecipient(string email)
    {
        if (!Recipients.Contains(email))
            Recipients.Add(email);
    }

    public void RemoveRecipient(string email)
    {
        Recipients.Remove(email);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void RecordRun()
    {
        LastRunAt = DateTimeOffset.UtcNow;
    }
}

public class AlertRule : Entity<Guid>
{
    public Guid DashboardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Metric { get; private set; } = string.Empty;
    public string Condition { get; private set; } = string.Empty; // >, >=, <, <=, ==, !=
    public decimal Threshold { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    private AlertRule() { }

    public static AlertRule Create(
        Guid dashboardId,
        string name,
        string description,
        string metric,
        string condition,
        decimal threshold)
    {
        return new AlertRule
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboardId,
            Name = name,
            Description = description,
            Metric = metric,
            Condition = condition,
            Threshold = threshold,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Trigger()
    {
        LastTriggeredAt = DateTimeOffset.UtcNow;
    }

    public bool CheckCondition(decimal currentValue)
    {
        return Condition switch
        {
            ">" => currentValue > Threshold,
            ">=" => currentValue >= Threshold,
            "<" => currentValue < Threshold,
            "<=" => currentValue <= Threshold,
            "==" => currentValue == Threshold,
            "!=" => currentValue != Threshold,
            _ => false
        };
    }
}
