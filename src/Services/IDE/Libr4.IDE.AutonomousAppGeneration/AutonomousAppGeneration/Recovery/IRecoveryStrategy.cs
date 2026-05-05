namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Interface for recovery strategies that can recover from LLM pipeline errors.
/// </summary>
public interface IRecoveryStrategy
{
    /// <summary>
    /// Determines if this strategy can recover from the given exception and context.
    /// </summary>
    bool CanRecover(Exception exception, RecoveryContext context);

    /// <summary>
    /// Attempts to recover from the error by modifying the context.
    /// </summary>
    Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of this recovery strategy for telemetry and logging.
    /// </summary>
    string GetStrategyName();
}

/// <summary>
/// Context information for recovery attempts.
/// </summary>
public class RecoveryContext
{
    public string CurrentPrompt { get; set; } = string.Empty;
    public List<string> MessageHistory { get; set; } = new();
    public int CurrentTokenCount { get; set; }
    public int MaxTokenLimit { get; set; }
    public int RecoveryAttempt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of a recovery attempt.
/// </summary>
public class RecoveryResult
{
    public bool Success { get; set; }
    public string StrategyUsed { get; set; } = string.Empty;
    public RecoveryContext ContextAfterRecovery { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
