namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class HermesMemoryManagerOptions
{
    public bool EnablePrefetch { get; set; } = true;

    public int PrefetchTopK { get; set; } = 8;

    public int MaxNudgesPerTurn { get; set; } = 5;

    public bool EnableToolIngest { get; set; } = true;

    public int MinToolOutputCharsForIngest { get; set; } = 40;

    public bool EnablePreCompactConsolidation { get; set; } = true;
}
