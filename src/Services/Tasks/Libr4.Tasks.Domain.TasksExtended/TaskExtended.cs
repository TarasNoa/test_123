using System;
using System.Collections.Generic;

namespace Libr4.Tasks.Domain.TasksExtended;

/// <summary>Task status enumeration</summary>
public enum TaskStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>Task priority enumeration</summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>Moderation status enumeration</summary>
public enum ModerationStatus
{
    Pending,
    Approved,
    Rejected,
    Flagged,
    UnderReview
}

/// <summary>Task category enumeration</summary>
public enum TaskCategory
{
    Development,
    Design,
    Writing,
    Marketing,
    Business,
    Support,
    Other
}

/// <summary>Recurring frequency enumeration</summary>
public enum RecurringFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly,
    Custom
}

/// <summary>Task draft entity</summary>
public class TaskDraft
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TaskCategory? Category { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public TaskPriority? Priority { get; set; }
    public bool IsRemote { get; set; } = true;
    public string? Location { get; set; }
    public List<string> SkillsRequired { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsPublishable() =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Description) &&
        Category.HasValue &&
        SkillsRequired.Count > 0;

    public bool IsExpired() =>
        ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}

/// <summary>Task template entity</summary>
public class TaskTemplate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskCategory Category { get; set; }
    public decimal? DefaultBudgetMin { get; set; }
    public decimal? DefaultBudgetMax { get; set; }
    public TaskPriority DefaultPriority { get; set; } = TaskPriority.Medium;
    public List<string> SkillsRequired { get; set; } = [];
    public int? DefaultDurationDays { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsPublic { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Milestone entity</summary>
public class Milestone
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public decimal? Budget { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Open;
    public List<string> Deliverables { get; set; } = [];
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Recurring task configuration</summary>
public class RecurringTaskConfig
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public RecurringFrequency Frequency { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public int? DayOfWeek { get; set; } // 0-6 for weekly
    public int? DayOfMonth { get; set; } // 1-31 for monthly
    public DateTimeOffset NextOccurrenceDate { get; set; }
    public int OccurrenceCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool ShouldCreateNext()
    {
        var now = DateTimeOffset.UtcNow;
        var withinEndDate = !EndDate.HasValue || now <= EndDate.Value;
        var withinMaxOccurrences = !MaxOccurrences.HasValue || OccurrenceCount < MaxOccurrences.Value;
        return IsActive && withinEndDate && withinMaxOccurrences && now >= NextOccurrenceDate;
    }

    public DateTimeOffset CalculateNextOccurrence() =>
        Frequency switch
        {
            RecurringFrequency.Daily => NextOccurrenceDate.AddDays(1),
            RecurringFrequency.Weekly => NextOccurrenceDate.AddDays(7),
            RecurringFrequency.BiWeekly => NextOccurrenceDate.AddDays(14),
            RecurringFrequency.Monthly => NextOccurrenceDate.AddMonths(1),
            RecurringFrequency.Quarterly => NextOccurrenceDate.AddMonths(3),
            RecurringFrequency.Yearly => NextOccurrenceDate.AddYears(1),
            RecurringFrequency.Custom => NextOccurrenceDate.AddDays(7), // Default to weekly for custom
            _ => NextOccurrenceDate
        };
}

/// <summary>Extended task aggregate root</summary>
public class TaskExtended
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskCategory Category { get; set; }
    public string? Subcategory { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Open;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public Guid CreatorId { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public bool IsRemote { get; set; } = true;
    public string? Location { get; set; }
    public List<string> SkillsRequired { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset? Deadline { get; set; }
    public double? EstimatedHours { get; set; }
    public int Complexity { get; set; } = 5; // 1-10
    public bool IsMultiUser { get; set; }
    public int MaxTeamSize { get; set; } = 1;
    public int CurrentTeamSize { get; set; }

    // Extended features
    public bool IsDraft { get; set; }
    public Guid? DraftId { get; set; }
    public Guid? TemplateId { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? RecurringConfigId { get; set; }
    public List<Milestone> Milestones { get; set; } = [];

    // AI Analysis
    public int? AiComplexityScore { get; set; }
    public decimal? AiSuggestedMinPrice { get; set; }
    public decimal? AiSuggestedMaxPrice { get; set; }
    public Dictionary<string, object> AiAnalysisData { get; set; } = [];
    public DateTimeOffset? AiAnalyzedAt { get; set; }

    // Metadata
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsOverdue() =>
        Deadline.HasValue && DateTimeOffset.UtcNow > Deadline.Value;

    public int? DaysUntilDeadline() =>
        Deadline.HasValue
            ? Math.Max(0, (int)(Deadline.Value - DateTimeOffset.UtcNow).TotalDays)
            : null;

    public void AddMilestone(string title, string? description, DateTimeOffset dueDate, decimal? budget, List<string> deliverables, DateTimeOffset now)
    {
        var milestone = new Milestone
        {
            Id = Guid.NewGuid(),
            TaskId = Id,
            Title = title,
            Description = description,
            DueDate = dueDate,
            Budget = budget,
            Status = TaskStatus.Open,
            Deliverables = deliverables,
            Order = Milestones.Count + 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        Milestones.Add(milestone);
        UpdatedAt = now;
    }

    public void CompleteMilestone(Guid milestoneId, DateTimeOffset now)
    {
        var milestone = Milestones.FirstOrDefault(m => m.Id == milestoneId);
        if (milestone != null)
        {
            milestone.Status = TaskStatus.Completed;
            milestone.UpdatedAt = now;
        }
        UpdatedAt = now;
    }

    public void SetMultiUser(int maxTeamSize, DateTimeOffset now)
    {
        IsMultiUser = true;
        MaxTeamSize = maxTeamSize;
        UpdatedAt = now;
    }

    public int CalculateComplexity()
    {
        var milestoneComplexity = Milestones.Count * 2;
        var teamComplexity = IsMultiUser ? MaxTeamSize * 3 : 0;
        return Math.Min(10, milestoneComplexity + teamComplexity);
    }
}

/// <summary>Task subcategory entity</summary>
public class TaskSubcategory
{
    public Guid Id { get; set; }
    public TaskCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int Order { get; set; }
}

/// <summary>Task tag entity</summary>
public class TaskTag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TaskCategory? Category { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
