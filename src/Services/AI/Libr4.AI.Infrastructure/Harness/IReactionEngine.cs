namespace Libr4.AI.Infrastructure.Harness;

/// <summary>
/// Reaction Engine - automatic reactions to CI failures, review comments, and agent lifecycle events
/// Based on Claude Octopus pattern
/// </summary>
public interface IReactionEngine
{
    /// <summary>
    /// Process an agent lifecycle event and trigger reactions
    /// </summary>
    Task ProcessEventAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get reaction configuration
    /// </summary>
    Task<ReactionConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update reaction configuration
    /// </summary>
    Task UpdateConfigurationAsync(ReactionConfiguration config, CancellationToken cancellationToken = default);
}

public class AgentLifecycleEvent
{
    public string AgentId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public LifecycleState State { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum LifecycleState
{
    Running,
    PrOpen,
    CiPending,
    CiFailed,
    ReviewPending,
    ChangesRequested,
    Approved,
    Mergeable,
    Merged,
    Done,
    Stuck
}

public class ReactionConfiguration
{
    public List<ReactionRule> Rules { get; set; } = new();
}

public class ReactionRule
{
    public LifecycleState Event { get; set; }
    public ReactionAction Action { get; set; }
    public int MaxRetries { get; set; }
    public int EscalateAfterMinutes { get; set; }
    public bool Enabled { get; set; } = true;
}

public enum ReactionAction
{
    ForwardLogs,
    ForwardComments,
    Escalate,
    Retry,
    Notify,
    Abort
}
