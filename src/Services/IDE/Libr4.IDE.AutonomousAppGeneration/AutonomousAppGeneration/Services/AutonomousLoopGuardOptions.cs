namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Safety controls for autonomous generation loop to prevent runaway execution.
/// Inspired by production agent loops (circuit breaker + bounded retries).
/// </summary>
public sealed class AutonomousLoopGuardOptions
{
    /// <summary>
    /// Open circuit when this many consecutive iterations produce no code updates.
    /// </summary>
    public int NoProgressThreshold { get; set; } = 3;

    /// <summary>
    /// Open circuit when the same normalized error signature repeats this many times.
    /// </summary>
    public int SameErrorThreshold { get; set; } = 3;

    /// <summary>
    /// After the same error signature repeats, run deterministic root-cause remediation before failing.
    /// </summary>
    public bool EnableRootCauseEscalation { get; set; } = true;

    /// <summary>
    /// P0-6 of audit roadmap: per-phase build failures abort the run by default.
    /// Set to <see cref="BuildGateBlockingMode.WarnOnly"/> to debug safety-net behaviour.
    /// </summary>
    public BuildGateBlockingMode BuildGateMode { get; set; } = BuildGateBlockingMode.StrictPerPhase;

    /// <summary>
    /// P1-3 of audit roadmap: when true the planning prefix of Handle is delegated to
    /// <see cref="Pipeline.IGenerationPipelineRunner"/> (stages Order 0-120) instead of
    /// being executed inline. Defaults to true; set to false to revert to legacy path.
    /// </summary>
    public bool UsePipelineRunnerForPlanningPrefix { get; set; } = true;

    /// <summary>
    /// When true, post-planning milestones are recorded via <see cref="Pipeline.IFullGenerationPipelineRunner"/>
    /// (strangler-fig migration off monolithic Handle). Defaults to true.
    /// </summary>
    public bool UsePipelineRunnerForFullHandle { get; set; } = true;

    /// <summary>
    /// Compile-only fix passes after generation (before the main test iteration loop).
    /// </summary>
    public int MaxStartupBuildRemediationPasses { get; set; } = 5;

    /// <summary>
    /// Run compile (build-only) before tests each iteration; fixes compile errors before test phase.
    /// </summary>
    public bool UseStagedBuildRetest { get; set; } = true;

    /// <summary>
    /// Extra compile-only verification passes after applying patches within one iteration (no extra iteration budget).
    /// </summary>
    public int MaxPostFixCompileVerifications { get; set; } = 2;

    /// <summary>
    /// When false, banking runs cannot complete with <c>fix_deferred_shadow_build</c> while shadow build is red.
    /// </summary>
    public bool AllowBankingBypassWithoutGreenBuild { get; set; } = false;

    /// <summary>
    /// Repair iterations 1..N use deterministic compile recovery only; iteration N+1 and above invoke the LLM fixer
    /// (after Level 0/2/3 quick wins). Prevents spinning on errors that need new methods/files.
    /// </summary>
    public int LlmFixerEscalationAfterIteration { get; set; } = 2;

    /// <summary>
    /// Claude Code-style surgical repair: build log + numbered files → search/replace edits.
    /// </summary>
    public bool UseClaudeCodeStyleRepair { get; set; } = true;

    /// <summary>
    /// First iteration that may invoke surgical LLM repair (after deterministic tiers).
    /// </summary>
    public int SurgicalRepairFromIteration { get; set; } = 1;

    /// <summary>
    /// Max search/replace edits per surgical repair attempt.
    /// </summary>
    public int MaxSurgicalEditsPerIteration { get; set; } = 6;

    /// <summary>
    /// Use FIM (fill-in-the-middle) infilling for large files with known error line.
    /// </summary>
    public bool UseFimRepair { get; set; } = true;

    /// <summary>
    /// Minimum source lines before FIM is preferred over full-file surgical JSON.
    /// </summary>
    public int FimMinFileLines { get; set; } = 200;

    /// <summary>
    /// Lines above/below the error line included in the FIM hole.
    /// </summary>
    public int FimHoleRadiusLines { get; set; } = 4;

    /// <summary>
    /// Resume fix-only runs skip generation gates/review2 and enter repair loop immediately.
    /// </summary>
    public bool ResumeFixOnlyFastPath { get; set; } = true;

    /// <summary>Prefer unified diff (apply_patch) over JSON search/replace in surgical repair.</summary>
    public bool UseApplyPatchRepair { get; set; } = true;
}
