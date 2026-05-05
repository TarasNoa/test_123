using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PlanQualityGateStage> _logger;

    public PlanQualityGateStage(
        IAutonomousQualityGateService qualityGates,
        ILogger<PlanQualityGateStage> logger)
    {
        _qualityGates = qualityGates;
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

        var reason =
            $"quality_gate_plan_failed: score={gate.Score}; reasons={string.Join(",", gate.Reasons)}";
        _logger.LogWarning(
            "[Pipeline] Plan quality gate failed for run {RunId}: {Reason}",
            context.Orchestrator.Id, reason);
        return Task.FromResult(StageOutcome.Stop(reason));
    }
}
