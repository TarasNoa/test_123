namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Iteration status from autoresearch
/// </summary>
public enum IterationStatus
{
    /// <summary>
    /// Baseline measurement
    /// </summary>
    Baseline,
    
    /// <summary>
    /// Change kept (improved)
    /// </summary>
    Keep,
    
    /// <summary>
    /// Change discarded (worse)
    /// </summary>
    Discard,
    
    /// <summary>
    /// Verification failed
    /// </summary>
    VerificationFailed,
    
    /// <summary>
    /// Crashed during execution
    /// </summary>
    Crashed
}

/// <summary>
/// Verification metrics for mechanical verification (from autoresearch)
/// </summary>
public class VerificationMetrics
{
    public double BaselineValue { get; set; }
    public double CurrentValue { get; set; }
    public double Delta { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public bool Improved => Delta > 0;
    
    public VerificationMetrics(double baselineValue, double currentValue, string metricName)
    {
        BaselineValue = baselineValue;
        CurrentValue = currentValue;
        Delta = currentValue - baselineValue;
        MetricName = metricName;
    }
}

/// <summary>
/// Single generate -> test -> fix iteration inside the orchestrator's main loop.
/// Each iteration has its own execution result and optional error reports
/// produced by the fixer agents.
/// Enhanced with mechanical verification and automatic rollback concepts from autoresearch.
/// </summary>
public sealed class IterationCycle
{
    public Guid Id { get; }
    public int Number { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; private set; }
    public ExecutionResult? Execution { get; private set; }
    public IReadOnlyList<ErrorReport> Errors => _errors.AsReadOnly();
    public IReadOnlyList<string> AppliedFixes => _appliedFixes.AsReadOnly();
    public IReadOnlyList<RetryEvent> RetryEvents => _retryEvents.AsReadOnly();
    public bool Succeeded => Execution?.Succeeded == true;
    
    /// <summary>
    /// Iteration status (from autoresearch)
    /// </summary>
    public IterationStatus Status { get; private set; }
    
    /// <summary>
    /// Verification metrics for mechanical verification
    /// </summary>
    public VerificationMetrics? Metrics { get; private set; }
    
    /// <summary>
    /// Git commit hash before verification (for rollback)
    /// </summary>
    public string? PreVerificationCommit { get; private set; }
    
    /// <summary>
    /// Git commit hash after verification
    /// </summary>
    public string? PostVerificationCommit { get; private set; }
    
    /// <summary>
    /// Whether this iteration was rolled back
    /// </summary>
    public bool WasRolledBack { get; private set; }
    
    /// <summary>
    /// Rollback reason
    /// </summary>
    public string? RollbackReason { get; private set; }

    private readonly List<ErrorReport> _errors = new();
    private readonly List<string> _appliedFixes = new();
    private readonly List<RetryEvent> _retryEvents = new();

    public IterationCycle(int number)
    {
        if (number < 1) throw new ArgumentOutOfRangeException(nameof(number), "Iteration number must be >= 1");
        Id = Guid.NewGuid();
        Number = number;
        StartedAt = DateTime.UtcNow;
        Status = IterationStatus.Baseline;
    }

    public void SetExecutionResult(ExecutionResult result)
    {
        Execution = result ?? throw new ArgumentNullException(nameof(result));
        CompletedAt = DateTime.UtcNow;
    }

    public void AddError(ErrorReport error)
    {
        if (error == null) return;
        _errors.Add(error);
    }

    public void RecordFix(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return;
        _appliedFixes.Add(description);
    }

    public void RecordRetry(int attempt, string reason, long backoffMs)
    {
        _retryEvents.Add(new RetryEvent(
            attempt: attempt,
            reason: reason,
            backoffMs: backoffMs,
            timestampUtc: DateTime.UtcNow));
    }
    
    /// <summary>
    /// Set verification metrics (mechanical verification from autoresearch)
    /// </summary>
    public void SetVerificationMetrics(VerificationMetrics metrics)
    {
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        Status = metrics.Improved ? IterationStatus.Keep : IterationStatus.Discard;
    }
    
    /// <summary>
    /// Record pre-verification commit for rollback capability
    /// </summary>
    public void SetPreVerificationCommit(string commitHash)
    {
        PreVerificationCommit = commitHash;
    }
    
    /// <summary>
    /// Record post-verification commit
    /// </summary>
    public void SetPostVerificationCommit(string commitHash)
    {
        PostVerificationCommit = commitHash;
    }
    
    /// <summary>
    /// Mark iteration as rolled back
    /// </summary>
    public void MarkAsRolledBack(string reason)
    {
        WasRolledBack = true;
        RollbackReason = reason;
        Status = IterationStatus.Discard;
    }
    
    /// <summary>
    /// Mark iteration as crashed
    /// </summary>
    public void MarkAsCrashed()
    {
        Status = IterationStatus.Crashed;
    }
    
    /// <summary>
    /// Mark verification as failed
    /// </summary>
    public void MarkVerificationFailed()
    {
        Status = IterationStatus.VerificationFailed;
    }
}
