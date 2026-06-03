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

        context.Plan = _validator.EnsureValidOrThrow(context.Plan);
        context.Orchestrator.RecordQualityGate(
            "plan_command_validation",
            10,
            true,
            new[] { "normalized_or_valid" });

        return Task.FromResult(StageOutcome.Continue);
    }
}
