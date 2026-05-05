using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PlanCommandValidationStage> _logger;

    public PlanCommandValidationStage(
        IPlanCommandValidator validator,
        ILogger<PlanCommandValidationStage> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public string Name => "plan_command_validation";
    public int Order => 110; // after planning (100), before generation (200)

    public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (context.Plan is null)
            return Task.FromResult(StageOutcome.Continue);

        var validation = _validator.Validate(context.Plan);
        if (validation.IsValid)
            return Task.FromResult(StageOutcome.Continue);

        var (safeBuild, safeTest) = _validator.GetSafeDefaults(context.Plan);
        _logger.LogWarning(
            "[AutoGen {Id}] Plan command validation failed ({Issues}). Substituting safe stack defaults.",
            context.Orchestrator.Id, string.Join(",", validation.Issues));

        context.Orchestrator.RecordQualityGate(
            "plan_command_validation",
            8,
            true,
            new[] { $"issues:{string.Join(",", validation.Issues)}", "fallback:safe_defaults_applied" });

        context.Plan = new GenerationPlan(
            context.Plan.ApplicationName,
            context.Plan.ApplicationDescription,
            context.Plan.TechStack,
            context.Plan.Phases,
            context.Plan.RequiredAgents,
            context.Plan.RuntimeImage,
            safeBuild,
            safeTest,
            context.Plan.MaxIterations);

        return Task.FromResult(StageOutcome.Continue);
    }
}
