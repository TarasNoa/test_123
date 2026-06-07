using Libr4.IDE.AutonomousAppGeneration.Agents;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Merges benchmark-mode overrides into multi-agent orchestration options.
/// </summary>
public static class BenchmarkOrchestrationOptionsResolver
{
    public static AgentOrchestrationOptions Resolve(
        AgentOrchestrationOptions baseline,
        AutonomousBenchmarkModeOptions benchmark)
    {
        if (!benchmark.EnableBenchmarkMode)
            return baseline;

        var incremental = baseline.UseIncrementalFileScopedGeneration;
        return new AgentOrchestrationOptions
        {
            RunPhasesInParallel = baseline.RunPhasesInParallel,
            ExcludeInfrastructurePhases = benchmark.ForceExcludeInfrastructurePhases || baseline.ExcludeInfrastructurePhases,
            UseFocusedFullStackPhases = baseline.UseFocusedFullStackPhases,
            SkipLlmReviewWhenParseableFiles = benchmark.SkipMultiAgentLlmReview || baseline.SkipLlmReviewWhenParseableFiles,
            MaxLlmReviewRounds = benchmark.SkipMultiAgentLlmReview ? 0 : baseline.MaxLlmReviewRounds,
            RunNestedSubtasksInParallel = incremental ? false : baseline.RunNestedSubtasksInParallel,
            UseParallelTasksPerPhase = baseline.UseParallelTasksPerPhase,
            MaxConcurrentTasks = baseline.MaxConcurrentTasks,
            UseIncrementalFileScopedGeneration = incremental,
            MaxFilesPerIncrementalTask = baseline.MaxFilesPerIncrementalTask,
            IncrementalEmptyBatchMaxRetries = Math.Max(baseline.IncrementalEmptyBatchMaxRetries, 2),
            MaxExistingFileContentChars = baseline.MaxExistingFileContentChars,
            MaxExistingFilesInPrompt = baseline.MaxExistingFilesInPrompt,
            SkipIncrementalTaskWhenTargetExists = baseline.SkipIncrementalTaskWhenTargetExists,
            MinCharsToSkipIncrementalTask = baseline.MinCharsToSkipIncrementalTask,
            UseExpandedJavaReactManifest = baseline.UseExpandedJavaReactManifest,
            IncrementalSeedMode = baseline.IncrementalSeedMode,
            RejectUnplannedGeneratedPaths = baseline.RejectUnplannedGeneratedPaths,
            RequiredManifestCoveragePercent = Math.Min(baseline.RequiredManifestCoveragePercent, 50)
        };
    }
}
