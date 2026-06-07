using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 stage example: runs the plan-command validator (P1-10) and substitutes
/// safe stack defaults if the plan contains malformed shell commands.
/// Same logic that currently lives inline in the handler; extracted here so the
/// strangler refactor can incrementally swap call sites.
/// </summary>
public sealed class PlanCommandValidationStage : IGenerationStage
{
    private readonly IPlanCommandValidator _validator;
    private readonly AutonomousBenchmarkModeOptions _benchmarkModeOptions;
    private readonly ILogger<PlanCommandValidationStage> _logger;

    public PlanCommandValidationStage(
        IPlanCommandValidator validator,
        IOptions<AutonomousBenchmarkModeOptions> benchmarkModeOptions,
        ILogger<PlanCommandValidationStage> logger)
    {
        _validator = validator;
        _benchmarkModeOptions = benchmarkModeOptions.Value;
        _logger = logger;
    }

    public string Name => "plan_command_validation";
    public int Order => 110; // after planning (100), before generation (200)

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Plan is null)
            return Task.FromResult(StageOutcome.Continue);

        context.Orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.Planning);

        var useSafeDefaults = _benchmarkModeOptions.EnableBenchmarkMode
                              && _benchmarkModeOptions.UseSafeDefaultsOnPlanValidationFailure;
        var before = context.Plan;
        var rawValidation = _validator.Validate(before);
        context.Plan = _validator.EnsureValidOrThrow(before, useSafeDefaults);
        var validation = _validator.Validate(context.Plan);

        var reasons = validation.IsValid
            ? rawValidation.IsValid
                ? new[] { "normalized_or_valid" }
                : new[] { "normalized_from_planner_commands", $"planner_issues={string.Join(",", rawValidation.Issues)}" }
            : new[]
            {
                "benchmark_safe_defaults_applied",
                $"issues={string.Join(",", validation.Issues)}"
            };

        if (!rawValidation.IsValid)
        {
            _logger.LogWarning(
                "[AutoGen {Id}] Planner build/test commands adjusted ({Issues}); using normalized plan commands.",
                context.Orchestrator.Id,
                string.Join(", ", rawValidation.Issues));
        }

        context.Orchestrator.RecordQualityGate(
            "plan_command_validation",
            10,
            true,
            reasons);

        return Task.FromResult(StageOutcome.Continue);
    }
}
