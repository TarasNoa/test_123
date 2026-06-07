namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public sealed class DreamConsolidationOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>UTC hour (0–23) for the nightly consolidation run.</summary>
    public int NightlyHourUtc { get; set; } = 3;

    public double MinScoreThreshold { get; set; } = 0.15;

    public int StaleAgeDays { get; set; } = 90;

    public double MinHashSimilarityThreshold { get; set; } = 0.82;

    public int MinEpisodicClusterSize { get; set; } = 2;
}
