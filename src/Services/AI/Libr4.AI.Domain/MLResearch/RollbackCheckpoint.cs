namespace Libr4.AI.Domain.MLResearch;

/// <summary>
/// Trigger for rollback
/// </summary>
public enum RollbackTrigger
{
    VerificationFailed,
    TestFailed,
    BenchmarkDegraded,
    ManualRequest,
    PerformanceDegradation,
    QualityGateFailed
}

/// <summary>
/// Status of rollback operation
/// </summary>
public enum RollbackStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// A checkpoint for rollback
/// </summary>
public class RollbackCheckpoint
{
    public string CheckpointId { get; set; } = string.Empty;
    public string ResearchTaskId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string StateSnapshot { get; set; } = string.Empty; // JSON serialized state
    public List<string> ModifiedFiles { get; set; } = new();
    public List<string> CreatedFiles { get; set; } = new();
    public List<string> DeletedFiles { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string? Description { get; set; }
    public bool IsAutomatic { get; set; }
    public string? TriggeredBy { get; set; }
    
    public RollbackCheckpoint()
    {
    }
    
    public RollbackCheckpoint(string checkpointId, string researchTaskId, string stateSnapshot)
    {
        CheckpointId = checkpointId;
        ResearchTaskId = researchTaskId;
        StateSnapshot = stateSnapshot;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add modified file
    /// </summary>
    public void AddModifiedFile(string filePath)
    {
        if (!ModifiedFiles.Contains(filePath))
        {
            ModifiedFiles.Add(filePath);
        }
    }
    
    /// <summary>
    /// Add created file
    /// </summary>
    public void AddCreatedFile(string filePath)
    {
        if (!CreatedFiles.Contains(filePath))
        {
            CreatedFiles.Add(filePath);
        }
    }
    
    /// <summary>
    /// Add deleted file
    /// </summary>
    public void AddDeletedFile(string filePath)
    {
        if (!DeletedFiles.Contains(filePath))
        {
            DeletedFiles.Add(filePath);
        }
    }
    
    /// <summary>
    /// Get total file count
    /// </summary>
    public int TotalFiles => ModifiedFiles.Count + CreatedFiles.Count + DeletedFiles.Count;
}
