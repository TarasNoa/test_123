namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// One repair attempt: error class → mechanism → outcome (for Recovery Efficiency dashboards).
/// </summary>
public sealed record RecoveryEfficiencyRecord(
    int IterationNumber,
    RecoveryRootCauseCategory RootCauseCategory,
    string PrimaryErrorClass,
    RecoveryMechanism Mechanism,
    int PatchesApplied,
    bool? BuildSucceededAfterRepair,
    DateTime AttemptedAtUtc,
    string? ErrorSignature = null,
    long? RepairDurationMs = null);
