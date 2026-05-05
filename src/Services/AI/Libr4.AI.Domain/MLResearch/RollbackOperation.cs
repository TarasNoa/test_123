namespace Libr4.AI.Domain.MLResearch;

/// <summary>
/// A rollback operation
/// </summary>
public class RollbackOperation
{
    public string OperationId { get; set; } = string.Empty;
    public RollbackTrigger Trigger { get; set; }
    public RollbackCheckpoint TargetCheckpoint { get; set; } = null!;
    public RollbackStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> RollbackLog { get; set; } = new();
    public List<string> FilesRestored { get; set; } = new();
    public List<string> FilesDeleted { get; set; } = new();
    public string? FailureReason { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : TimeSpan.Zero;
    
    public RollbackOperation()
    {
    }
    
    public RollbackOperation(string operationId, RollbackTrigger trigger, RollbackCheckpoint targetCheckpoint)
    {
        OperationId = operationId;
        Trigger = trigger;
        TargetCheckpoint = targetCheckpoint;
        Status = RollbackStatus.NotStarted;
    }
    
    /// <summary>
    /// Mark operation as started
    /// </summary>
    public void MarkAsStarted()
    {
        Status = RollbackStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        RollbackLog.Add($"Rollback started at {StartedAt:yyyy-MM-dd HH:mm:ss}");
    }
    
    /// <summary>
    /// Mark operation as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = RollbackStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        RollbackLog.Add($"Rollback completed at {CompletedAt:yyyy-MM-dd HH:mm:ss}");
        RollbackLog.Add($"Duration: {Duration.TotalSeconds:F2}s");
        RollbackLog.Add($"Files restored: {FilesRestored.Count}");
        RollbackLog.Add($"Files deleted: {FilesDeleted.Count}");
    }
    
    /// <summary>
    /// Mark operation as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = RollbackStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        FailureReason = reason;
        RollbackLog.Add($"Rollback failed: {reason}");
    }
    
    /// <summary>
    /// Mark operation as cancelled
    /// </summary>
    public void MarkAsCancelled()
    {
        Status = RollbackStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        RollbackLog.Add("Rollback cancelled");
    }
    
    /// <summary>
    /// Add log entry
    /// </summary>
    public void AddLog(string message)
    {
        RollbackLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
    }
    
    /// <summary>
    /// Add restored file
    /// </summary>
    public void AddRestoredFile(string filePath)
    {
        if (!FilesRestored.Contains(filePath))
        {
            FilesRestored.Add(filePath);
            AddLog($"Restored file: {filePath}");
        }
    }
    
    /// <summary>
    /// Add deleted file
    /// </summary>
    public void AddDeletedFile(string filePath)
    {
        if (!FilesDeleted.Contains(filePath))
        {
            FilesDeleted.Add(filePath);
            AddLog($"Deleted file: {filePath}");
        }
    }
    
    /// <summary>
    /// Check if operation succeeded
    /// </summary>
    public bool Succeeded => Status == RollbackStatus.Completed;
    
    /// <summary>
    /// Check if operation failed
    /// </summary>
    public bool Failed => Status == RollbackStatus.Failed;
}
