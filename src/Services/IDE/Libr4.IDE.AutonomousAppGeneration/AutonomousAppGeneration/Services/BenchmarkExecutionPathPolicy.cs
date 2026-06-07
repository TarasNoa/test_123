using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Benchmark execution path: Planning → Generation → Build → Repair, with optional LLM enhancers as best-effort.
/// When <see cref="AutonomousPlatformUtilizationOptions.EnableFullPlatformUtilization"/> is true,
/// benchmark shortcuts are disabled even if benchmark mode is configured.
/// </summary>
public static class BenchmarkExecutionPathPolicy
{
    public static class Stages
    {
        public const string Planning = "planning";
        public const string CommandValidation = "command_validation";
        public const string PlanQualityGate = "plan_quality_gate";
        public const string Generation = "generation";
        public const string StartupBuild = "startup_build";
        public const string RepairLoop = "repair_loop";
        public const string CascadePlanning = "cascade_planning";
        public const string SecurityReview = "security_review";
        public const string ReviewGate2 = "review_gate_2";
        public const string MultiAgentReview = "multi_agent_review";
        public const string ProductionReadiness = "production_readiness";
        public const string Verify = "verify";
        public const string Ship = "ship";
    }

    public static bool IsActive(
        AutonomousBenchmarkModeOptions options,
        AutonomousPlatformUtilizationOptions? platform = null) =>
        IsBenchmarkShortcutActive(options, platform);

    public static bool IsActive(AutonomousBenchmarkModeOptions options) =>
        options.EnableBenchmarkMode && options.UseBenchmarkExecutionPath;

    public static BenchmarkStageCriticality GetCriticality(
        AutonomousBenchmarkModeOptions options,
        string stage,
        AutonomousPlatformUtilizationOptions? platform = null)
    {
        if (!IsBenchmarkShortcutActive(options, platform))
            return BenchmarkStageCriticality.Required;

        return stage switch
        {
            Stages.Planning => BenchmarkStageCriticality.Required,
            Stages.CommandValidation => BenchmarkStageCriticality.Required,
            Stages.Generation => BenchmarkStageCriticality.Required,
            Stages.StartupBuild => BenchmarkStageCriticality.Required,
            Stages.RepairLoop => BenchmarkStageCriticality.Required,
            Stages.CascadePlanning => BenchmarkStageCriticality.Optional,
            Stages.SecurityReview => BenchmarkStageCriticality.Optional,
            Stages.ReviewGate2 => BenchmarkStageCriticality.Optional,
            Stages.MultiAgentReview => BenchmarkStageCriticality.Optional,
            Stages.ProductionReadiness => BenchmarkStageCriticality.Optional,
            Stages.PlanQualityGate => BenchmarkStageCriticality.Optional,
            Stages.Verify => BenchmarkStageCriticality.Optional,
            Stages.Ship => BenchmarkStageCriticality.Optional,
            _ => BenchmarkStageCriticality.Required
        };
    }

    /// <summary>Benchmark path uses deterministic cascade (no LLM DAG pass).</summary>
    public static bool UseDeterministicCascadeOnly(
        AutonomousBenchmarkModeOptions options,
        AutonomousPlatformUtilizationOptions? platform = null) =>
        IsBenchmarkShortcutActive(options, platform)
        && GetCriticality(options, Stages.CascadePlanning, platform) == BenchmarkStageCriticality.Optional;

    public static bool ShouldDeferFailedGate(
        AutonomousBenchmarkModeOptions options,
        string stage,
        AutonomousPlatformUtilizationOptions? platform = null) =>
        IsBenchmarkShortcutActive(options, platform)
        && GetCriticality(options, stage, platform) == BenchmarkStageCriticality.Optional;

    public static bool ShouldFallbackOnLlmInfrastructureFailure(
        AutonomousBenchmarkModeOptions options,
        string stage,
        Exception ex,
        AutonomousPlatformUtilizationOptions? platform = null)
    {
        if (!IsBenchmarkShortcutActive(options, platform))
            return false;

        if (GetCriticality(options, stage, platform) != BenchmarkStageCriticality.Optional)
            return false;

        if (ex is AutonomousGenerationFailedException)
            return true;

        if (ex is HttpRequestException)
            return true;

        var message = ex.Message;
        return message.Contains("BadGateway", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
               || message.Contains("API call failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBenchmarkShortcutActive(
        AutonomousBenchmarkModeOptions benchmark,
        AutonomousPlatformUtilizationOptions? platform) =>
        PlatformUtilizationPolicy.IsBenchmarkShortcutPathActive(
            benchmark,
            platform ?? new AutonomousPlatformUtilizationOptions());
}
