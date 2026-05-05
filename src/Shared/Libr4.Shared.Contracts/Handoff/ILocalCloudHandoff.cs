namespace Libr4.Shared.Contracts.Handoff;

/// <summary>
/// Represents a handoff between local and cloud execution.
/// </summary>
public record HandoffRequest
{
    /// <summary>
    /// Unique identifier for the handoff.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Current execution environment.
    /// </summary>
    public ExecutionEnvironment CurrentEnvironment { get; init; }

    /// <summary>
    /// Target execution environment.
    /// </summary>
    public ExecutionEnvironment TargetEnvironment { get; init; }

    /// <summary>
    /// Context or state to transfer.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();

    /// <summary>
    /// Reason for the handoff.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Priority of the handoff.
    /// </summary>
    public HandoffPriority Priority { get; init; } = HandoffPriority.Normal;

    /// <summary>
    /// When the handoff was requested.
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Handoff metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Execution environment.
/// </summary>
public enum ExecutionEnvironment
{
    Local,
    Cloud,
    Hybrid
}

/// <summary>
/// Priority of a handoff.
/// </summary>
public enum HandoffPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Result of a handoff operation.
/// </summary>
public record HandoffResult
{
    /// <summary>
    /// Handoff request ID.
    /// </summary>
    public string HandoffId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the handoff succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// New environment after handoff.
    /// </summary>
    public ExecutionEnvironment NewEnvironment { get; init; }

    /// <summary>
    /// Transferred context.
    /// </summary>
    public Dictionary<string, object> TransferredContext { get; init; } = new();

    /// <summary>
    /// Error message if the handoff failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the handoff started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the handoff completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Handoff duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Interface for local/cloud handoff service.
/// Provides enhanced local/cloud handoff for long runs.
/// </summary>
public interface ILocalCloudHandoff
{
    /// <summary>
    /// Initiates a handoff to a different execution environment.
    /// </summary>
    /// <param name="request">Handoff request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handoff result.</returns>
    Task<HandoffResult> InitiateHandoffAsync(
        HandoffRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a handoff.
    /// </summary>
    /// <param name="handoffId">Handoff ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handoff status.</returns>
    Task<HandoffStatus> GetHandoffStatusAsync(
        string handoffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending handoff.
    /// </summary>
    /// <param name="handoffId">Handoff ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancelled, false if not found or already completed.</returns>
    Task<bool> CancelHandoffAsync(
        string handoffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current execution environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current execution environment.</returns>
    Task<ExecutionEnvironment> GetCurrentEnvironmentAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a handoff is recommended based on current conditions.
    /// </summary>
    /// <param name="context">Evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handoff recommendation, or null if no handoff is recommended.</returns>
    Task<HandoffRecommendation?> EvaluateHandoffNeedAsync(
        HandoffEvaluationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of a handoff.
/// </summary>
public enum HandoffStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Context for evaluating handoff need.
/// </summary>
public record HandoffEvaluationContext
{
    /// <summary>
    /// Current execution environment.
    /// </summary>
    public ExecutionEnvironment CurrentEnvironment { get; init; }

    /// <summary>
    /// Current resource utilization (0-1).
    /// </summary>
    public double ResourceUtilization { get; init; }

    /// <summary>
    /// Estimated remaining time in minutes.
    /// </summary>
    public int? EstimatedRemainingMinutes { get; init; }

    /// <summary>
    /// Current task complexity (0-1).
    /// </summary>
    public double TaskComplexity { get; init; }

    /// <summary>
    /// Network latency in milliseconds.
    /// </summary>
    public int? NetworkLatencyMs { get; init; }

    /// <summary>
    /// Additional context metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Handoff recommendation.
/// </summary>
public record HandoffRecommendation
{
    /// <summary>
    /// Recommended target environment.
    /// </summary>
    public ExecutionEnvironment TargetEnvironment { get; init; }

    /// <summary>
    /// Reason for the recommendation.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Estimated benefit of the handoff.
    /// </summary>
    public string EstimatedBenefit { get; init; } = string.Empty;
}

/// <summary>
/// In-memory implementation of local/cloud handoff service.
/// </summary>
public class InMemoryLocalCloudHandoff : ILocalCloudHandoff
{
    private readonly Dictionary<string, HandoffRequest> _handoffRequests = new();
    private readonly Dictionary<string, HandoffStatus> _handoffStatuses = new();
    private ExecutionEnvironment _currentEnvironment = ExecutionEnvironment.Local;

    public Task<HandoffResult> InitiateHandoffAsync(
        HandoffRequest request,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        
        _handoffRequests[request.Id] = request;
        _handoffStatuses[request.Id] = HandoffStatus.InProgress;

        // Simulate handoff
        var duration = TimeSpan.FromMilliseconds(new Random().Next(100, 500));
        var completedAt = startedAt + duration;

        _currentEnvironment = request.TargetEnvironment;
        _handoffStatuses[request.Id] = HandoffStatus.Completed;

        var result = new HandoffResult
        {
            HandoffId = request.Id,
            Success = true,
            NewEnvironment = request.TargetEnvironment,
            TransferredContext = request.Context,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = duration
        };

        return Task.FromResult(result);
    }

    public Task<HandoffStatus> GetHandoffStatusAsync(
        string handoffId,
        CancellationToken cancellationToken = default)
    {
        _handoffStatuses.TryGetValue(handoffId, out var status);
        return Task.FromResult(status);
    }

    public Task<bool> CancelHandoffAsync(
        string handoffId,
        CancellationToken cancellationToken = default)
    {
        if (!_handoffStatuses.ContainsKey(handoffId))
        {
            return Task.FromResult(false);
        }

        var currentStatus = _handoffStatuses[handoffId];
        if (currentStatus == HandoffStatus.Completed || currentStatus == HandoffStatus.Failed)
        {
            return Task.FromResult(false);
        }

        _handoffStatuses[handoffId] = HandoffStatus.Cancelled;
        return Task.FromResult(true);
    }

    public Task<ExecutionEnvironment> GetCurrentEnvironmentAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentEnvironment);
    }

    public Task<HandoffRecommendation?> EvaluateHandoffNeedAsync(
        HandoffEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        // High resource utilization -> recommend cloud
        if (context.ResourceUtilization > 0.8 && context.CurrentEnvironment == ExecutionEnvironment.Local)
        {
            return Task.FromResult<HandoffRecommendation?>(new HandoffRecommendation
            {
                TargetEnvironment = ExecutionEnvironment.Cloud,
                Reason = "High resource utilization on local environment",
                Confidence = 0.8,
                EstimatedBenefit = "Better performance and scalability"
            });
        }

        // Long running task -> recommend cloud
        if (context.EstimatedRemainingMinutes > 60 && context.CurrentEnvironment == ExecutionEnvironment.Local)
        {
            return Task.FromResult<HandoffRecommendation?>(new HandoffRecommendation
            {
                TargetEnvironment = ExecutionEnvironment.Cloud,
                Reason = "Long running task detected",
                Confidence = 0.7,
                EstimatedBenefit = "Cloud can handle long running tasks more reliably"
            });
        }

        // High network latency -> recommend local
        if (context.NetworkLatencyMs > 500 && context.CurrentEnvironment == ExecutionEnvironment.Cloud)
        {
            return Task.FromResult<HandoffRecommendation?>(new HandoffRecommendation
            {
                TargetEnvironment = ExecutionEnvironment.Local,
                Reason = "High network latency detected",
                Confidence = 0.6,
                EstimatedBenefit = "Local execution will be faster"
            });
        }

        // No handoff recommended
        return Task.FromResult<HandoffRecommendation?>(null);
    }
}
