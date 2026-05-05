using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Projects;

public enum ProjectStatus
{
    Draft,
    Active,
    OnHold,
    Completed,
    Cancelled
}

public enum ProjectPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class Project : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public ProjectPriority Priority { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? Deadline { get; private set; }
    public decimal Budget { get; private set; }
    public decimal Spent { get; private set; }
    public float Progress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ProjectMember> _members = new();
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private readonly List<ProjectTask> _tasks = new();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    private Project() { }

    public static Project Create(
        string name,
        string description,
        Guid ownerId,
        DateTime startDate,
        decimal budget,
        ProjectPriority priority = ProjectPriority.Medium)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Status = ProjectStatus.Draft,
            Priority = priority,
            OwnerId = ownerId,
            StartDate = startDate,
            Budget = budget,
            Spent = 0,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        Status = ProjectStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ProjectStatus.Completed;
        Progress = 100;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = ProjectStatus.Cancelled;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PutOnHold()
    {
        Status = ProjectStatus.OnHold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMember(ProjectMember member)
    {
        _members.Add(member);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMember(Guid memberId)
    {
        _members.RemoveAll(m => m.Id == memberId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTask(ProjectTask task)
    {
        _tasks.Add(task);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(float progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSpending(decimal amount)
    {
        Spent += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDeadline(DateTime deadline)
    {
        Deadline = deadline;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverBudget => Spent > Budget;
    public bool IsOverdue => Deadline.HasValue && Deadline < DateTime.UtcNow && Status != ProjectStatus.Completed;
}

public class ProjectMember : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty; // Owner, Manager, Developer, Designer, etc.
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid userId, string role)
    {
        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateRole(string role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

public class ProjectTask : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectTaskStatus Status { get; private set; }
    public ProjectTaskPriority Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? AssignedToId { get; private set; }
    public int EstimatedHours { get; private set; }
    public int ActualHours { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<TaskDependency> _dependencies = new();
    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();

    private ProjectTask() { }

    public static ProjectTask Create(
        Guid projectId,
        string title,
        string description,
        ProjectTaskPriority priority = ProjectTaskPriority.Medium,
        int estimatedHours = 0)
    {
        return new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = ProjectTaskStatus.Todo,
            Priority = priority,
            EstimatedHours = estimatedHours,
            ActualHours = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AssignTo(Guid userId)
    {
        AssignedToId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = ProjectTaskStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ProjectTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDependency(TaskDependency dependency)
    {
        _dependencies.Add(dependency);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordHours(int hours)
    {
        ActualHours += hours;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && Status != ProjectTaskStatus.Completed;
}

public class TaskDependency : Entity<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid DependsOnTaskId { get; private set; }
    public DependencyType Type { get; private set; }

    private TaskDependency() { }

    public static TaskDependency Create(Guid taskId, Guid dependsOnTaskId, DependencyType type = DependencyType.FinishToStart)
    {
        return new TaskDependency
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            DependsOnTaskId = dependsOnTaskId,
            Type = type
        };
    }
}

public enum ProjectTaskStatus
{
    Todo,
    InProgress,
    Review,
    Completed,
    Cancelled
}

public enum ProjectTaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum DependencyType
{
    FinishToStart,
    StartToStart,
    FinishToFinish,
    StartToFinish
}
