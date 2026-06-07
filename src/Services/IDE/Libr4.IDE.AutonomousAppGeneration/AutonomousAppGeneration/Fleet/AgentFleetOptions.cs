namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class AgentFleetOptions
{
    public const string SectionName = "AgentFleet";

    public string IndexDbPath { get; set; } = ".logs/agent-fleet-index.db";
    public string RunsRoot { get; set; } = ".logs/runs";

    /// <summary>Alert when Repairing run has no tool activity for this many minutes.</summary>
    public int StuckRepairingMinutes { get; set; } = 30;
}
