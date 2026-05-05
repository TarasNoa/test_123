using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Projects;

public sealed class Project : AggregateRoot<Guid>
{
    private readonly List<ProjectMember> _members = new();
    private readonly List<ProjectTask> _tasks = new();
    private readonly List<Milestone> _milestones = new();

    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Category { get; private set; } = "";
    public decimal? BudgetMin { get; private set; }
    public decimal? BudgetMax { get; private set; }
    public decimal? Budget { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Client { get; private set; }
    public DateTimeOffset? Deadline { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public int TeamSize { get; private set; }
    public int MaxTeamSize { get; private set; } = 5;
    public int Progress { get; private set; } // 0-100%
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();

    private Project() { }

    public static Project Create(
        string title,
        string description,
        string category,
        Guid ownerId,
        decimal? budgetMin,
        decimal? budgetMax,
        string currency,
        string? client,
        DateTimeOffset? deadline,
        DateTimeOffset now)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category.Trim(),
            BudgetMin = budgetMin,
            BudgetMax = budgetMax,
            Budget = budgetMax ?? budgetMin,
            Currency = currency.ToUpperInvariant(),
            Client = client?.Trim(),
            Deadline = deadline,
            Status = ProjectStatus.Planning,
            OwnerId = ownerId,
            TeamSize = 1,
            MaxTeamSize = 5,
            Progress = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddMember(Guid userId, string role, DateTimeOffset now)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this project");

        if (_members.Count >= MaxTeamSize)
            throw new DomainException("Project team is at maximum capacity");

        _members.Add(new ProjectMember(Id, userId, role, now));
        TeamSize = _members.Count + 1;
        UpdatedAt = now;
    }

    public void RemoveMember(Guid userId, DateTimeOffset now)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new DomainException("User is not a member of this project");

        _members.Remove(member);
        TeamSize = _members.Count + 1;
        UpdatedAt = now;
    }

    public void AddTask(string title, string description, Guid? assignedTo, ProjectTaskPriority priority, DateTimeOffset? dueDate, DateTimeOffset now)
    {
        var task = new ProjectTask(Id, title, description, assignedTo, priority, dueDate, now);
        _tasks.Add(task);
        UpdatedAt = now;
    }

    public void AddMilestone(string title, string description, DateTimeOffset dueDate, DateTimeOffset now)
    {
        var milestone = new Milestone(Id, title, description, dueDate, now);
        _milestones.Add(milestone);
        UpdatedAt = now;
    }

    public void UpdateProgress(int progress, DateTimeOffset now)
    {
        if (progress < 0 || progress > 100)
            throw new DomainException("Progress must be between 0 and 100");

        Progress = progress;
        UpdatedAt = now;
    }

    public void UpdateStatus(ProjectStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void Update(string title, string description, string category, decimal? budgetMin, decimal? budgetMax, 
        string currency, string? client, DateTimeOffset? deadline, DateTimeOffset now)
    {
        Title = title.Trim();
        Description = description.Trim();
        Category = category.Trim();
        BudgetMin = budgetMin;
        BudgetMax = budgetMax;
        Budget = budgetMax ?? budgetMin;
        Currency = currency.ToUpperInvariant();
        Client = client?.Trim();
        Deadline = deadline;
        UpdatedAt = now;
    }
}

public sealed class ProjectMember
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "";
    public DateTimeOffset JoinedAt { get; private set; }

    private ProjectMember() { }

    internal ProjectMember(Guid projectId, Guid userId, string role, DateTimeOffset now)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role.Trim();
        JoinedAt = now;
    }

    public void UpdateRole(string role, DateTimeOffset now)
    {
        Role = role.Trim();
    }
}

public sealed class ProjectTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public Guid? AssignedToId { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public ProjectTaskPriority Priority { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ProjectTask() { }

    internal ProjectTask(Guid projectId, string title, string description, Guid? assignedTo, 
        ProjectTaskPriority priority, DateTimeOffset? dueDate, DateTimeOffset now)
    {
        ProjectId = projectId;
        Title = title.Trim();
        Description = description.Trim();
        AssignedToId = assignedTo;
        Status = ProjectTaskStatus.Todo;
        Priority = priority;
        DueDate = dueDate;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void UpdateStatus(ProjectTaskStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void UpdateAssignment(Guid? assignedTo, DateTimeOffset now)
    {
        AssignedToId = assignedTo;
        UpdatedAt = now;
    }
}

public sealed class Milestone
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public DateTimeOffset DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Milestone() { }

    internal Milestone(Guid projectId, string title, string description, DateTimeOffset dueDate, DateTimeOffset now)
    {
        ProjectId = projectId;
        Title = title.Trim();
        Description = description.Trim();
        DueDate = dueDate;
        IsCompleted = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        IsCompleted = true;
        UpdatedAt = now;
    }
}

public enum ProjectStatus
{
    Planning = 0,
    InProgress = 1,
    Review = 2,
    Completed = 3,
    OnHold = 4,
    Cancelled = 5
}

public enum ProjectTaskStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Completed = 3,
    Blocked = 4,
    Cancelled = 5
}

public enum ProjectTaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
