namespace Libr4.AI.Infrastructure.Orchestration;

/// <summary>
/// Background Agent Orchestrator - manages autonomous agent execution
/// Based on Level 7 of "8 levels of agent engineering"
/// </summary>
public interface IBackgroundAgentOrchestrator
{
    /// <summary>
    /// Dispatch task to background agent
    /// </summary>
    Task<Guid> DispatchAsync(AgentTask task);
    
    /// <summary>
    /// Get task status
    /// </summary>
    Task<AgentTaskStatus?> GetTaskStatusAsync(Guid taskId);
    
    /// <summary>
    /// Wait for task completion
    /// </summary>
    Task<AgentTaskResult?> WaitForCompletionAsync(Guid taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel task
    /// </summary>
    Task CancelTaskAsync(Guid taskId);
    
    /// <summary>
    /// List active tasks
    /// </summary>
    Task<List<AgentTaskStatus>> ListActiveTasksAsync();
    
    /// <summary>
    /// Run parallel agents on same task (Level 8 pattern)
    /// </summary>
    Task<List<AgentTaskResult>> RunParallelAgentsAsync(
        AgentTask task,
        int agentCount,
        CancellationToken cancellationToken = default);
}

public class AgentTask
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Context { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? ModelId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int MaxIterations { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 300;
}

public class AgentTaskStatus
{
    public Guid TaskId { get; set; }
    public TaskState State { get; set; }
    public int CurrentIteration { get; set; }
    public string? CurrentStep { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum TaskState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class AgentTaskResult
{
    public Guid TaskId { get; set; }
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int IterationsCompleted { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
