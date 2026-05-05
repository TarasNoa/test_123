using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Gantt;

public enum TaskDependencyType
{
    FinishToStart,
    StartToStart,
    FinishToFinish,
    StartToFinish
}

public class GanttChart : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public Guid ProjectId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<GanttTask> _tasks = new();
    public IReadOnlyCollection<GanttTask> Tasks => _tasks.AsReadOnly();

    private readonly List<GanttDependency> _dependencies = new();
    public IReadOnlyCollection<GanttDependency> Dependencies => _dependencies.AsReadOnly();

    private GanttChart() { }

    public static GanttChart Create(string name, Guid projectId, DateTime startDate, DateTime endDate)
    {
        return new GanttChart
        {
            Id = Guid.NewGuid(),
            Name = name,
            ProjectId = projectId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddTask(GanttTask task)
    {
        _tasks.Add(task);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            _tasks.Remove(task);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddDependency(GanttDependency dependency)
    {
        _dependencies.Add(dependency);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class GanttTask : Entity<Guid>
{
    public Guid GanttChartId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int Duration { get; private set; }  // in days
    public int Progress { get; private set; }  // 0-100
    public string? AssignedToId { get; private set; }
    public string Color { get; private set; } = "#000000";

    private GanttTask() { }

    public static GanttTask Create(Guid ganttChartId, string name, DateTime startDate, DateTime endDate, string? description = null, string? assignedToId = null)
    {
        return new GanttTask
        {
            Id = Guid.NewGuid(),
            GanttChartId = ganttChartId,
            Name = name,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Duration = (endDate - startDate).Days,
            Progress = 0,
            AssignedToId = assignedToId,
            Color = "#3b82f6"
        };
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Max(0, Math.Min(100, progress));
    }

    public void UpdateDateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        Duration = (endDate - startDate).Days;
    }

    public void AssignTo(string? userId)
    {
        AssignedToId = userId;
    }

    public void SetColor(string color)
    {
        Color = color;
    }
}

public class GanttDependency : Entity<Guid>
{
    public Guid GanttChartId { get; private set; }
    public Guid PredecessorTaskId { get; private set; }
    public Guid SuccessorTaskId { get; private set; }
    public TaskDependencyType Type { get; private set; }
    public int Lag { get; private set; }  // in days

    private GanttDependency() { }

    public static GanttDependency Create(Guid ganttChartId, Guid predecessorTaskId, Guid successorTaskId, TaskDependencyType type = TaskDependencyType.FinishToStart, int lag = 0)
    {
        return new GanttDependency
        {
            Id = Guid.NewGuid(),
            GanttChartId = ganttChartId,
            PredecessorTaskId = predecessorTaskId,
            SuccessorTaskId = successorTaskId,
            Type = type,
            Lag = lag
        };
    }

    public void UpdateLag(int lag)
    {
        Lag = lag;
    }

    public void UpdateType(TaskDependencyType type)
    {
        Type = type;
    }
}
