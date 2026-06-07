namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class FleetRetentionOptions
{
    public const string SectionName = "AgentFleet:Retention";

    public bool EnableHostedSweep { get; set; } = true;

    public int SweepIntervalHours { get; set; } = 12;

    /// <summary>Auto-archive terminal fleet entries after this many days (default 365).</summary>
    public int FleetIndexArchiveAfterDays { get; set; } = 365;

    /// <summary>Delete on-disk run artifacts for archived runs after this many days (default 90).</summary>
    public int RunArtifactsDeleteAfterDays { get; set; } = 90;

    /// <summary>Documented alignment with Hermes episodic retention (see HermesMemory:EpisodicRetentionDays).</summary>
    public int EpisodicMemoryRetentionDays { get; set; } = 90;
}

public sealed record FleetRetentionSweepResult(
    int ArchivedCount,
    int ArtifactsPurgedCount,
    int EpisodicMemoryPrunedCount);
