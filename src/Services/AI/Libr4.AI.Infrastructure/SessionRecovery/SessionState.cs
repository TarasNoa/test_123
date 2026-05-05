namespace Libr4.AI.Infrastructure.SessionRecovery;

/// <summary>
/// External state management for LLM sessions - makes agent stateless and deterministic
/// Based on SESSION_RECOVERY protocol pattern
/// </summary>
public class SessionState
{
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    
    /// <summary>
    /// Current task stack - hot working set (max 150 lines)
    /// </summary>
    public List<SessionTask> Tasks { get; set; } = new();
    
    /// <summary>
    /// Event log of all directives - Write-Ahead Log
    /// </summary>
    public List<PromptEvent> PromptHistory { get; set; } = new();
    
    /// <summary>
    /// Archive for completed tasks to avoid cluttering context
    /// </summary>
    public List<SessionTask> CompletedTasks { get; set; } = new();
}

public enum TaskStatus
{
    Planning,
    InProgress,
    Blocked,
    Completed,
    Failed
}

public class SessionTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PromptEvent
{
    public Guid Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}
