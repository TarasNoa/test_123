using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Static check result from build/lint/test pipeline.
/// </summary>
public sealed record StaticCheckResult(
    string CheckName,
    bool Passed,
    int IssueCount,
    IReadOnlyList<string> Issues);

/// <summary>
/// Architecture checklist item evaluation.
/// </summary>
public sealed record ArchitectureChecklistItem(
    string ItemId,
    string Description,
    bool Satisfied,
    string? Evidence,
    string? RemediationHint);

/// <summary>
/// Regression guard baseline comparison.
/// </summary>
public sealed record RegressionGuardResult(
    bool IsRegression,
    double MetricDelta,
    string MetricName,
    string? BaselineValue,
    string? CurrentValue);

/// <summary>
/// Comprehensive review gate decision with operational details.
/// </summary>
public sealed record ReviewGateDecision(
    string Stage,
    bool Passed,
    int OverallScore,
    IReadOnlyList<StaticCheckResult> StaticChecks,
    IReadOnlyList<ArchitectureChecklistItem> ArchitectureChecklist,
    IReadOnlyList<RegressionGuardResult> RegressionGuards,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RemediationHints,
    DateTime EvaluatedAtUtc);

/// <summary>
/// Service for comprehensive review gate evaluation with static checks, architecture validation, and regression detection.
/// </summary>
public interface IReviewGate2Service
{
    /// <summary>
    /// Evaluate generated artifacts with static checks aggregation.
    /// </summary>
    IReadOnlyList<StaticCheckResult> EvaluateStaticChecks(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan);

    /// <summary>
    /// Evaluate architecture checklist against generated code.
    /// </summary>
    IReadOnlyList<ArchitectureChecklistItem> EvaluateArchitectureChecklist(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan);

    /// <summary>
    /// Detect regressions by comparing against baseline metrics.
    /// </summary>
    IReadOnlyList<RegressionGuardResult> DetectRegressions(
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<QualityGateResult> baselineMetrics,
        GenerationPlan plan);

    /// <summary>
    /// Produce comprehensive review gate decision combining all checks.
    /// </summary>
    ReviewGateDecision EvaluateComprehensive(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<QualityGateResult> baselineMetrics);
}
