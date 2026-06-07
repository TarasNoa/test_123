using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class VerifyStage : IGenerationStage
{
    private readonly IVerifySubagentService _verify;
    private readonly AutonomousBenchmarkModeOptions _benchmarkOptions;
    private readonly AutonomousPlatformUtilizationOptions _platformOptions;
    private readonly VerifySubagentOptions _verifyOptions;
    private readonly ILogger<VerifyStage> _logger;

    public VerifyStage(
        IVerifySubagentService verify,
        IOptions<AutonomousBenchmarkModeOptions> benchmarkOptions,
        IOptions<AutonomousPlatformUtilizationOptions> platformOptions,
        IOptions<VerifySubagentOptions> verifyOptions,
        ILogger<VerifyStage> logger)
    {
        _verify = verify;
        _benchmarkOptions = benchmarkOptions.Value;
        _platformOptions = platformOptions.Value;
        _verifyOptions = verifyOptions.Value;
        _logger = logger;
    }

    public string Name => "verify";
    public int Order => 450;

    public async Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        var result = await _verify.RunAsync(context, ct).ConfigureAwait(false);
        context.Items["verify_stage_reached"] = true;

        if (result.Skipped)
        {
            _logger.LogInformation(
                "[VerifyStage] Skipped for run {RunId}: {Reason}",
                context.Orchestrator.Id,
                result.SkipReason);
            return StageOutcome.Continue;
        }

        context.Orchestrator.RecordQualityGate(
            "verify_subagent",
            result.Passed ? 5 : 1,
            result.Passed,
            new[] { result.Summary });

        if (result.Passed)
            return StageOutcome.Continue;

        if (BenchmarkExecutionPathPolicy.ShouldDeferFailedGate(
                _benchmarkOptions,
                BenchmarkExecutionPathPolicy.Stages.Verify,
                _platformOptions))
        {
            context.Orchestrator.RecordQualityGate(
                "verify_deferred_benchmark",
                2,
                true,
                new[] { result.Summary });
            return StageOutcome.Continue;
        }

        if (!_verifyOptions.RequirePassInProduction)
            return StageOutcome.Continue;

        return StageOutcome.Stop("verify_not_passed");
    }
}
