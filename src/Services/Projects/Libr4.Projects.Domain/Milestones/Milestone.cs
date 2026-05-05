using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Milestones;

public enum MilestoneStatus
{
    NotStarted,
    InProgress,
    Completed,
    Overdue,
    Cancelled
}

public class Milestone : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateTime TargetDate { get; private set; }
    public DateTime? ActualCompletionDate { get; private set; }
    public MilestoneStatus Status { get; private set; }
    public int Progress { get; private set; }  // 0-100
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Guid> _taskIds = new();
    public IReadOnlyCollection<Guid> TaskIds => _taskIds.AsReadOnly();

    private Milestone() { }

    public static Milestone Create(string name, Guid projectId, DateTime targetDate, string? description = null)
    {
        return new Milestone
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ProjectId = projectId,
            TargetDate = targetDate,
            Status = MilestoneStatus.NotStarted,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status == MilestoneStatus.NotStarted)
        {
            Status = MilestoneStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Complete()
    {
        Status = MilestoneStatus.Completed;
        Progress = 100;
        ActualCompletionDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Max(0, Math.Min(100, progress));
        
        if (Progress > 0 && Status == MilestoneStatus.NotStarted)
        {
            Status = MilestoneStatus.InProgress;
        }
        
        if (Progress == 100)
        {
            Status = MilestoneStatus.Completed;
            ActualCompletionDate = DateTime.UtcNow;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsOverdue()
    {
        if (Status != MilestoneStatus.Completed && DateTime.UtcNow > TargetDate)
        {
            Status = MilestoneStatus.Overdue;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Cancel()
    {
        Status = MilestoneStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTask(Guid taskId)
    {
        if (!_taskIds.Contains(taskId))
        {
            _taskIds.Add(taskId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTask(Guid taskId)
    {
        if (_taskIds.Contains(taskId))
        {
            _taskIds.Remove(taskId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateTargetDate(DateTime targetDate)
    {
        TargetDate = targetDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue => Status != MilestoneStatus.Completed && DateTime.UtcNow > TargetDate;
}
