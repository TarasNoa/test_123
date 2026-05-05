using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AILearningPaths;

public enum PathDifficulty { Beginner, Intermediate, Advanced, Expert }
public enum PathStatus { NotStarted, InProgress, Completed, Paused }

public class LearningPath
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PathDifficulty Difficulty { get; set; }
    public List<string> Goals { get; set; } = new List<string>();
    public List<LearningModule> Modules { get; set; } = new List<LearningModule>();
    public PathStatus Status { get; set; } = PathStatus.NotStarted;
    public int EstimatedHours { get; set; }
    public float ProgressPercentage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class LearningModule
{
    public Guid Id { get; set; }
    public Guid PathId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Resources { get; set; } = new List<string>();
    public List<string> Skills { get; set; } = new List<string>();
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
