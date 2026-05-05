namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>How the per-phase build gate reacts to failures (P0-6).</summary>
public enum BuildGateBlockingMode
{
    /// <summary>Failed per-phase build aborts the run with a quality_gate_build_failed result.</summary>
    StrictPerPhase = 0,

    /// <summary>Legacy mode: log a warning and continue (used only for safety-net debugging).</summary>
    WarnOnly = 1
}

public sealed class AutonomousQualityGateOptions
{
    /// <summary>
    /// When true, compares plan name/description/phases to generated sources for obvious intent gaps
    /// (e.g. auth mentioned but no JWT/Authorize/UseAuthentication patterns).
    /// </summary>
    public bool EnableIntentHeuristics { get; set; } = true;

    public int PlanMinScore { get; set; } = 9;
    public int ConsistencyMinScore { get; set; } = 9;
    public int GenerationMinScore { get; set; } = 9;
    public int BuildMinScore { get; set; } = 9;
    public int ExecutionMinScore { get; set; } = 9;
    public int FixMinScore { get; set; } = 9;

    /// <summary>
    /// Default-blocking per-phase build gate (P0-6 of audit). Use <see cref="BuildGateBlockingMode.WarnOnly"/>
    /// only for safety-net debugging where you intentionally want the run to proceed past failed builds.
    /// </summary>
    public BuildGateBlockingMode BuildGateMode { get; set; } = BuildGateBlockingMode.StrictPerPhase;
}
