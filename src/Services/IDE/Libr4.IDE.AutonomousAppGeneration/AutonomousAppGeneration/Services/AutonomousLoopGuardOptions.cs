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
    public int SameErrorThreshold { get; set; } = 5;

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
}
