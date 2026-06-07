namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class HermesMemoryOptions
{
    public string DbPath { get; set; } = ".logs/agent-runtime/hermes-memory.db";

    /// <summary>L0 episodic retention in days. Semantic/procedural/strategic/meta are permanent.</summary>
    public int EpisodicRetentionDays { get; set; } = 90;

    public bool EnableRetentionPrune { get; set; } = true;
}
