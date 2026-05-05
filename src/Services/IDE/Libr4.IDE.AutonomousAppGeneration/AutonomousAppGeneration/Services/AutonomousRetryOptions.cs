namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Retry controls for transient execution failures inside shadow runtime.
/// </summary>
public sealed class AutonomousRetryOptions
{
    /// <summary>
    /// Max attempts for one execution run (initial try + retries).
    /// </summary>
    public int MaxExecutionAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff between retries.
    /// </summary>
    public int BaseBackoffMs { get; set; } = 500;
}
