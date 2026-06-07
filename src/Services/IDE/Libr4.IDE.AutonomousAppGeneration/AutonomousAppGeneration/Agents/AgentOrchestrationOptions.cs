namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Tunables for multi-agent / subagent orchestration (speed vs strictness).
/// </summary>
public sealed class AgentOrchestrationOptions
{
    public const string SectionName = "AutonomousAppGeneration:MultiAgent";

    /// <summary>When true, phases Backend/Frontend/Database run concurrently.</summary>
    public bool RunPhasesInParallel { get; set; }

    /// <summary>Skip DevOps/Observability/CICD/Documentation agents (major time saver).</summary>
    public bool ExcludeInfrastructurePhases { get; set; } = true;

    /// <summary>For Java+React and similar, only Backend, Frontend, Database.</summary>
    public bool UseFocusedFullStackPhases { get; set; } = true;

    /// <summary>Approve implementer output when JSON files parse; skip LLM spec/quality review.</summary>
    public bool SkipLlmReviewWhenParseableFiles { get; set; } = true;

    /// <summary>0 = no LLM review; 1 = spec only; 2 = spec + quality (legacy strict mode).</summary>
    public int MaxLlmReviewRounds { get; set; }

    /// <summary>Run child subtasks in parallel instead of sequentially.</summary>
    public bool RunNestedSubtasksInParallel { get; set; } = true;

    /// <summary>Split each phase into multiple parallel implementer tasks.</summary>
    public bool UseParallelTasksPerPhase { get; set; }

    /// <summary>Max parallel tasks per phase and max orchestrator concurrency.</summary>
    public int MaxConcurrentTasks { get; set; } = 1;

    /// <summary>
    /// One LLM call per target file (or tiny batch), persist after each task, inject workspace context.
    /// </summary>
    public bool UseIncrementalFileScopedGeneration { get; set; } = true;

    /// <summary>When &gt; 1, group related manifest paths into one LLM call (same parent folder).</summary>
    public int MaxFilesPerIncrementalTask { get; set; } = 1;

    /// <summary>Group Django/Spring/FastAPI feature files (models+views+urls+tests) into one agent session.</summary>
    public bool UseFeatureScopedGeneration { get; set; } = true;

    /// <summary>Retries per incremental task when LLM returns no files (then split multi-file batches).</summary>
    public int IncrementalEmptyBatchMaxRetries { get; set; } = 2;

    /// <summary>Max chars of each existing file body included in prompts (paths always listed).</summary>
    public int MaxExistingFileContentChars { get; set; } = 2_500;

    /// <summary>Max existing files injected into a single task prompt.</summary>
    public int MaxExistingFilesInPrompt { get; set; } = 10;

    /// <summary>Skip file-scoped LLM call when the target path already exists with enough content.</summary>
    public bool SkipIncrementalTaskWhenTargetExists { get; set; } = true;

    /// <summary>Minimum trimmed content length to treat an existing target as complete (per-path overrides apply).</summary>
    public int MinCharsToSkipIncrementalTask { get; set; } = 80;

    /// <summary>Use the detailed ~60-path Java+React manifest; other stacks always use the universal per-file manifest.</summary>
    public bool UseExpandedJavaReactManifest { get; set; } = true;

    /// <summary>None = LLM-only; MinimalSpine = few bootstrap files; FullSafetyNet = legacy ~25 seed.</summary>
    public IncrementalSeedMode IncrementalSeedMode { get; set; } = IncrementalSeedMode.None;

    /// <summary>Drop LLM output whose relativePath is not in the planned manifest.</summary>
    public bool RejectUnplannedGeneratedPaths { get; set; } = true;

    /// <summary>Required share of planned manifest paths present after generation (0-100).</summary>
    public int RequiredManifestCoveragePercent { get; set; } = 75;
}
