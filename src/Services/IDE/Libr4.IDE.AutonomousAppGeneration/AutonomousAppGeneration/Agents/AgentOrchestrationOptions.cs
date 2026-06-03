namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Tunables for multi-agent / subagent orchestration (speed vs strictness).
/// </summary>
public sealed class AgentOrchestrationOptions
{
    public const string SectionName = "AutonomousAppGeneration:MultiAgent";

    /// <summary>When true, phases Backend/Frontend/Database run concurrently.</summary>
    public bool RunPhasesInParallel { get; set; } = true;

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
    public bool UseParallelTasksPerPhase { get; set; } = true;

    /// <summary>Max parallel tasks per phase and max orchestrator concurrency.</summary>
    public int MaxConcurrentTasks { get; set; } = 4;
}
