namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Status of pipeline execution
/// </summary>
public enum PipelineExecutionStatus
{
    NotStarted,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled,
    RollingBack
}

/// <summary>
/// Options for pipeline execution
/// </summary>
public class PipelineExecutionOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableAutoRollback { get; set; } = true;
    public bool EnableQualityGates { get; set; } = true;
    public CertaintyThreshold MinimumCertainty { get; set; } = new();
    public bool EnableParallelPhases { get; set; } = false;
    public TimeSpan PhaseTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool StopOnFirstFailure { get; set; } = true;
    
    public PipelineExecutionOptions()
    {
        MinimumCertainty = new CertaintyThreshold(CertaintyLevel.Medium);
    }
}

/// <summary>
/// Result of a single phase execution
/// </summary>
public class PhaseExecutionResult
{
    public string PhaseId { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public PhaseStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int RetryCount { get; set; }
    public bool PassedQualityGates { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public bool Succeeded => Status == PhaseStatus.Completed;
    public bool Failed => Status == PhaseStatus.Failed;
}

/// <summary>
/// Result of pipeline execution
/// </summary>
public class PipelineExecutionResult
{
    public string PipelineId { get; set; } = string.Empty;
    public PipelineExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
    public List<PhaseExecutionResult> PhaseResults { get; set; } = new();
    public List<AgentDecision> Decisions { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool Succeeded => Status == PipelineExecutionStatus.Completed;
    public bool Failed => Status == PipelineExecutionStatus.Failed;
    public bool WasRolledBack { get; set; }
    public string? RollbackReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Get successful phases
    /// </summary>
    public List<PhaseExecutionResult> SuccessfulPhases => 
        PhaseResults.Where(p => p.Succeeded).ToList();
    
    /// <summary>
    /// Get failed phases
    /// </summary>
    public List<PhaseExecutionResult> FailedPhases => 
        PhaseResults.Where(p => p.Failed).ToList();
    
    /// <summary>
    /// Get total retry count
    /// </summary>
    public int TotalRetryCount => 
        PhaseResults.Sum(p => p.RetryCount);
    
    /// <summary>
    /// Mark pipeline as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = PipelineExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark pipeline as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = PipelineExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Errors.Add(reason);
    }
    
    /// <summary>
    /// Mark pipeline as rolling back
    /// </summary>
    public void MarkAsRollingBack(string reason)
    {
        Status = PipelineExecutionStatus.RollingBack;
        RollbackReason = reason;
    }
    
    /// <summary>
    /// Add phase result
    /// </summary>
    public void AddPhaseResult(PhaseExecutionResult result)
    {
        PhaseResults.Add(result);
    }
    
    /// <summary>
    /// Add decision
    /// </summary>
    public void AddDecision(AgentDecision decision)
    {
        Decisions.Add(decision);
    }
}
