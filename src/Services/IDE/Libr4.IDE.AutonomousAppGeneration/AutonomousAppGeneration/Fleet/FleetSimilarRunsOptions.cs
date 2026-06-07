namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class FleetSimilarRunsOptions
{
    public const string SectionName = "AutonomousAppGeneration:FleetSimilarRuns";

    public bool Enabled { get; set; } = true;
    public string CollectionId { get; set; } = "libr4_fleet_sessions";
    public int DefaultLimit { get; set; } = 8;
    public double MinScore { get; set; } = 0.35;
}
