using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// P1-1 of audit roadmap. Replaces substring-based ReviewGate2 checks with
/// stack-aware semantic analysis. Each rule:
///   * declares which stacks it applies to (so a Python plan doesn't run Roslyn);
///   * returns Pass/Fail with a stable identifier ReviewGate2 already maps to checklist items;
///   * is independently unit-testable.
///
/// New rules land here, are registered in DI, and ReviewGate2 will consult them in addition
/// to (eventually replacing) its current text matchers.
/// </summary>
public interface IArchitectureCheckRule
{
    /// <summary>Stable identifier matching ReviewGate2 checklist item id where applicable.</summary>
    string CheckId { get; }

    /// <summary>True if this rule should be evaluated for the supplied plan.</summary>
    bool AppliesTo(GenerationPlan plan);

    Task<ArchitectureCheckOutcome> EvaluateAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct);
}

public sealed record ArchitectureCheckOutcome(
    string CheckId,
    bool Satisfied,
    string? Detail = null,
    string? RemediationHint = null,
    IReadOnlyList<string>? EvidencePaths = null);
