using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Failure signature pattern for detecting repeated failures.
/// </summary>
public sealed record FailureSignature(
    string Stage,
    IReadOnlyList<string> ReasonPatterns,
    int OccurrenceCount,
    DateTime LastObservedUtc);

/// <summary>
/// Recovery task recommendation with stage-specific guidance.
/// </summary>
public sealed record RecoveryTaskRecommendation(
    string TaskId,
    string Stage,
    string Description,
    IReadOnlyList<string> RecommendedActions,
    string Rationale);

/// <summary>
/// Service for adaptive re-planning after quality gate failures.
/// Detects repeated failure signatures and generates stage-specific recovery tasks.
/// </summary>
public interface IAdaptiveReplannerService
{
    /// <summary>
    /// Analyze failure history and detect repeated signatures.
    /// </summary>
    IReadOnlyList<FailureSignature> DetectFailureSignatures(
        IReadOnlyList<QualityGateResult> gateHistory);

    /// <summary>
    /// Generate recovery task recommendations based on detected signatures.
    /// Prevents duplicate/looping recovery tasks.
    /// </summary>
    IReadOnlyList<RecoveryTaskRecommendation> GenerateRecoveryTasks(
        IReadOnlyList<FailureSignature> signatures,
        IReadOnlyList<AgentTaskGraphEntry> currentGraph);

    /// <summary>
    /// Check if a recovery task would create a loop or duplicate.
    /// </summary>
    bool WouldCreateLoop(
        RecoveryTaskRecommendation task,
        IReadOnlyList<AgentTaskGraphEntry> currentGraph);
}
