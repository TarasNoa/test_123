namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class HumanReviewOptions
{
    /// <summary>When true, ShipStage waits for explicit human approval of all generated files.</summary>
    public bool RequireHumanReview { get; set; } = true;

    /// <summary>Auto-spawn repair subagent when files are rejected or repair is requested.</summary>
    public bool AutoSpawnRepairOnReject { get; set; } = true;
}
