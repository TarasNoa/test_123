using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 stage: evaluates the plan via <see cref="IAutonomousQualityGateService"/>,
/// records the gate result on the orchestrator, and stops the pipeline if the
/// gate is not passing. Mirrors the inline plan-gate block in the legacy handler.
///
/// Order=120 — runs after plan generation (100) and command validation (110).
/// </summary>
public sealed class PlanQualityGateStage : IGenerationStage
{
    private readonly IAutonomousQualityGateService _qualityGates;
    private readonly AutonomousBenchmarkModeOptions _benchmarkModeOptions;
    private readonly AutonomousPlatformUtilizationOptions _platformOptions;
    private readonly ILogger<PlanQualityGateStage> _logger;

    public PlanQualityGateStage(
        IAutonomousQualityGateService qualityGates,
        IOptions<AutonomousBenchmarkModeOptions> benchmarkModeOptions,
        IOptions<AutonomousPlatformUtilizationOptions> platformOptions,
        ILogger<PlanQualityGateStage> logger)
    {
        _qualityGates = qualityGates;
        _benchmarkModeOptions = benchmarkModeOptions.Value;
        _platformOptions = platformOptions.Value;
        _logger = logger;
    }

    public string Name => "plan_quality_gate";
    public int Order => 120;

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Plan is null)
            return Task.FromResult(StageOutcome.Stop("plan_missing_for_quality_gate"));

        var gate = _qualityGates.EvaluatePlan(context.Plan);
        context.Orchestrator.RecordQualityGate(gate.Stage, gate.Score, gate.Passed, gate.Reasons);

        if (gate.Passed)
            return Task.FromResult(StageOutcome.Continue);

        if (BenchmarkExecutionPathPolicy.ShouldDeferFailedGate(
                _benchmarkModeOptions,
                BenchmarkExecutionPathPolicy.Stages.PlanQualityGate,
                _platformOptions))
        {
            context.Orchestrator.RecordQualityGate(
                "plan_quality_gate_deferred_benchmark",
                gate.Score,
                true,
                gate.Reasons.Concat(new[] { "benchmark_execution_path:plan_quality_deferred" }).ToArray());
            return Task.FromResult(StageOutcome.Continue);
        }

        var reason =
            $"quality_gate_plan_failed: score={gate.Score}; reasons={string.Join(",", gate.Reasons)}";
        _logger.LogWarning(
            "[Pipeline] Plan quality gate failed for run {RunId}: {Reason}",
            context.Orchestrator.Id, reason);
        return Task.FromResult(StageOutcome.Stop(reason));
    }
}
