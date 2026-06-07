namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>First actionable build failure in a run (generator defect signal vs repair signal).</summary>
public sealed record RunFirstFailureSnapshot(
    string ErrorClass,
    string RootCauseCategory,
    int IterationNumber,
    bool? RecoveredAfterRepair);
