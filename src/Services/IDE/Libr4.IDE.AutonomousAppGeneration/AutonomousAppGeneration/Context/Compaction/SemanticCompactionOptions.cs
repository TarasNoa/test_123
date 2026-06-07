namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public sealed class SemanticCompactionOptions
{
    public bool EnableSemanticCompaction { get; set; } = true;

    /// <summary>Trigger compaction when estimated chars exceed this ratio of char budget.</summary>
    public double TriggerBudgetRatio { get; set; } = 0.80;

    public int PreserveLastToolResults { get; set; } = 3;

    public int MinTurnsBeforeCompaction { get; set; } = 8;

    /// <summary>Use LLM summarizer when true; otherwise heuristic extractor (tests/default).</summary>
    public bool UseLlmSummarizer { get; set; } = false;
}
