namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Relaxed pipeline for multi-stack benchmarks (measure recovery, not production checklist).
/// </summary>
public sealed class AutonomousBenchmarkModeOptions
{
    public const string SectionName = "AutonomousAppGeneration:BenchmarkMode";

    public bool EnableBenchmarkMode { get; set; }

    /// <summary>
    /// Narrow path for recovery KPIs: required stages only; optional LLM enhancers are best-effort or skipped.
    /// </summary>
    public bool UseBenchmarkExecutionPath { get; set; } = true;

    /// <summary>Skip Review Gate 2 (observability, CI, architecture floor) so runs reach build/repair.</summary>
    public bool SkipReviewGate2 { get; set; } = true;

    /// <summary>Record security review as deferred instead of failing orchestration on JSON contract errors.</summary>
    public bool DeferSecurityReviewFailures { get; set; } = true;

    /// <summary>Best-effort security in benchmark: skip LLM security review when the call throws (timeout, BadGateway, etc.).</summary>
    public bool SkipSecurityReviewOnLlmFailure { get; set; } = true;

    /// <summary>Force multi-agent to skip DevOps/Observability/CICD/Documentation even if appsettings disables it.</summary>
    public bool ForceExcludeInfrastructurePhases { get; set; } = true;

    /// <summary>Disable LLM spec/quality review rounds during multi-agent generation.</summary>
    public bool SkipMultiAgentLlmReview { get; set; } = true;

    /// <summary>Skip production-readiness scoring gates that block before build.</summary>
    public bool RelaxProductionReadinessGates { get; set; } = true;

    /// <summary>Record production-readiness as advisory only (no blocking).</summary>
    public bool DeferProductionReadinessScoring { get; set; } = true;

    /// <summary>Allow generation gate to pass when only structural minimums are missing.</summary>
    public bool SkipStrictGenerationGate { get; set; } = true;

    /// <summary>Run ManifestRepairEngine before LLM on generation gate failure.</summary>
    public bool ApplyManifestRepairOnGenerationGateFailure { get; set; } = true;

    /// <summary>Substitute stack-safe build/test commands when plan validation fails (reach build/repair, not planner quirks).</summary>
    public bool UseSafeDefaultsOnPlanValidationFailure { get; set; } = true;

    /// <summary>
    /// Do not fail multi-agent generation when manifest coverage is below threshold but structural minimum is met.
    /// </summary>
    public bool DeferManifestCoverageGateFailure { get; set; } = true;
}
