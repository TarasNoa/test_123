namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>How a pipeline stage behaves when <see cref="AutonomousBenchmarkModeOptions.UseBenchmarkExecutionPath"/> is on.</summary>
public enum BenchmarkStageCriticality
{
    /// <summary>Failure aborts the run (measure core path only).</summary>
    Required,

    /// <summary>LLM/auxiliary stage: deterministic fallback or skip on failure (best effort).</summary>
    Optional,

    /// <summary>Stage not run in benchmark execution path.</summary>
    Disabled
}
