namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public sealed class FineTuningDataPipelineOptions
{
    public const string SectionName = "AutonomousAppGeneration:FineTuning";

    public bool Enabled { get; set; } = true;

    public bool AutoExtractCompletedRuns { get; set; } = true;

    public string DatasetsRoot { get; set; } = ".libr4/fine-tuning/datasets";

    public string SignaturesIndexPath { get; set; } = ".libr4/fine-tuning/signatures.jsonl";

    public double MinReadabilityScore { get; set; } = 0.35;

    public double MinHashDedupThreshold { get; set; } = 0.92;

    public int MinOutputChars { get; set; } = 200;

    public int MaxOutputChars { get; set; } = 120_000;
}
