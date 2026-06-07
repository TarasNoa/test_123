namespace Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;

public sealed class AutonomousBatchLlmProfileOptions
{
    public const string SectionName = "AutonomousAppGeneration:BatchLlmProfile";

    /// <summary>When true (or trigger source=ci/nightly), use cheaper non-streaming LLM profile for the run.</summary>
    public bool UseBatchLlmProfile { get; set; }

    public string Model { get; set; } = "openai/gpt-4o-mini";

    public bool DisableStreaming { get; set; } = true;

    /// <summary>Auto-enable benchmark execution path when batch profile is active.</summary>
    public bool EnableBenchmarkModeWithBatch { get; set; } = true;
}

public sealed class AutonomousBatchCiOptions
{
    public const string SectionName = "AutonomousAppGeneration:BatchCi";

    public bool EnableNightlyRegression { get; set; } = true;

    public int MaxIterations { get; set; } = 8;

    public string TriggerSource { get; set; } = "nightly-ci";
}
