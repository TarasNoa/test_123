namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Monotonic pipeline milestones for benchmark dashboards (furthest stage reached before fail/complete).
/// </summary>
public static class AutonomousPipelineStages
{
    public const string Planning = "Planning";
    public const string Generation = "Generation";
    public const string Security = "Security";
    public const string ReviewGate2 = "ReviewGate2";
    public const string StartupBuild = "StartupBuild";
    public const string RepairLoop = "RepairLoop";
    public const string Completed = "Completed";

    private static readonly string[] Ordered =
    [
        Planning,
        Generation,
        Security,
        ReviewGate2,
        StartupBuild,
        RepairLoop,
        Completed
    ];

    public static int GetOrder(string stage)
    {
        for (var i = 0; i < Ordered.Length; i++)
        {
            if (string.Equals(Ordered[i], stage, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Recovery KPIs (repair attempts, patchesApplied) are meaningful only after the first startup build pass.
    /// </summary>
    public static bool IsRecoveryMeasurementEligible(string? pipelineStageReached) =>
        GetOrder(pipelineStageReached ?? string.Empty) >= GetOrder(StartupBuild);
}
