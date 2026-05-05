namespace Libr4.AI.Domain.MLResearch;

/// <summary>
/// Status of research task
/// </summary>
public enum ResearchStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Status of research subtask
/// </summary>
public enum SubtaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// A research task with automatic verification and rollback
/// </summary>
public class ResearchTask
{
    public string TaskId { get; set; } = string.Empty;
    public string ResearchQuestion { get; set; } = string.Empty;
    public List<ResearchSubtask> Subtasks { get; set; } = new();
    public ResearchStatus Status { get; set; }
    public MechanicalVerificationPlan? VerificationPlan { get; set; }
    public List<RollbackCheckpoint> Checkpoints { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Result { get; set; }
    public List<string> Artifacts { get; set; } = new();
    public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : TimeSpan.Zero;
    
    public ResearchTask()
    {
    }
    
    public ResearchTask(string taskId, string researchQuestion)
    {
        TaskId = taskId;
        ResearchQuestion = researchQuestion;
        Status = ResearchStatus.NotStarted;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a subtask
    /// </summary>
    public void AddSubtask(ResearchSubtask subtask)
    {
        Subtasks.Add(subtask);
    }
    
    /// <summary>
    /// Get subtasks in execution order
    /// </summary>
    public List<ResearchSubtask> GetOrderedSubtasks()
    {
        return Subtasks.OrderBy(s => s.Order).ToList();
    }
    
    /// <summary>
    /// Get next executable subtask
    /// </summary>
    public ResearchSubtask? GetNextExecutableSubtask()
    {
        return GetOrderedSubtasks()
            .FirstOrDefault(s => s.Status == SubtaskStatus.NotStarted && s.Dependencies.All(dep => IsSubtaskCompleted(dep)));
    }
    
    /// <summary>
    /// Check if subtask is completed
    /// </summary>
    private bool IsSubtaskCompleted(string subtaskId)
    {
        return Subtasks.FirstOrDefault(s => s.SubtaskId == subtaskId)?.Status == SubtaskStatus.Completed;
    }
    
    /// <summary>
    /// Mark task as started
    /// </summary>
    public void MarkAsStarted()
    {
        Status = ResearchStatus.InProgress;
    }
    
    /// <summary>
    /// Mark task as completed
    /// </summary>
    public void MarkAsCompleted(string result)
    {
        Status = ResearchStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
    }
    
    /// <summary>
    /// Mark task as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = ResearchStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Result = $"Failed: {reason}";
    }
    
    /// <summary>
    /// Add a checkpoint
    /// </summary>
    public void AddCheckpoint(RollbackCheckpoint checkpoint)
    {
        Checkpoints.Add(checkpoint);
    }
    
    /// <summary>
    /// Add an artifact
    /// </summary>
    public void AddArtifact(string artifactPath)
    {
        if (!Artifacts.Contains(artifactPath))
        {
            Artifacts.Add(artifactPath);
        }
    }
    
    /// <summary>
    /// Get task progress
    /// </summary>
    public double GetProgress()
    {
        if (Subtasks.Count == 0) return 0.0;
        var completed = Subtasks.Count(s => s.Status == SubtaskStatus.Completed);
        return (double)completed / Subtasks.Count;
    }
}

/// <summary>
/// A subtask within a research task
/// </summary>
public class ResearchSubtask
{
    public string SubtaskId { get; set; } = string.Empty;
    public string SubtaskDescription { get; set; } = string.Empty;
    public SubtaskStatus Status { get; set; }
    public string? Result { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Order { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : TimeSpan.Zero;
    public List<string> OutputFiles { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public ResearchSubtask()
    {
    }
    
    public ResearchSubtask(string subtaskId, string subtaskDescription, int order = 0)
    {
        SubtaskId = subtaskId;
        SubtaskDescription = subtaskDescription;
        Order = order;
        Status = SubtaskStatus.NotStarted;
    }
    
    /// <summary>
    /// Add a dependency
    /// </summary>
    public void AddDependency(string subtaskId)
    {
        if (!Dependencies.Contains(subtaskId))
        {
            Dependencies.Add(subtaskId);
        }
    }
    
    /// <summary>
    /// Mark subtask as started
    /// </summary>
    public void MarkAsStarted()
    {
        Status = SubtaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark subtask as completed
    /// </summary>
    public void MarkAsCompleted(string result)
    {
        Status = SubtaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
    }
    
    /// <summary>
    /// Mark subtask as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = SubtaskStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Result = $"Failed: {reason}";
    }
    
    /// <summary>
    /// Mark subtask as skipped
    /// </summary>
    public void MarkAsSkipped(string reason)
    {
        Status = SubtaskStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
        Result = $"Skipped: {reason}";
    }
    
    /// <summary>
    /// Add output file
    /// </summary>
    public void AddOutputFile(string filePath)
    {
        if (!OutputFiles.Contains(filePath))
        {
            OutputFiles.Add(filePath);
        }
    }
}
