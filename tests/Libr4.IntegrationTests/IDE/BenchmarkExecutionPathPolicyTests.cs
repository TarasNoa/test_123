using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BenchmarkExecutionPathPolicyTests
{
    [Fact]
    public void UseDeterministicCascadeOnly_when_benchmark_execution_path_enabled()
    {
        var options = new AutonomousBenchmarkModeOptions
        {
            EnableBenchmarkMode = true,
            UseBenchmarkExecutionPath = true
        };

        BenchmarkExecutionPathPolicy.UseDeterministicCascadeOnly(options).Should().BeTrue();
        BenchmarkExecutionPathPolicy.GetCriticality(options, BenchmarkExecutionPathPolicy.Stages.CascadePlanning)
            .Should().Be(BenchmarkStageCriticality.Optional);
        BenchmarkExecutionPathPolicy.GetCriticality(options, BenchmarkExecutionPathPolicy.Stages.RepairLoop)
            .Should().Be(BenchmarkStageCriticality.Required);
    }

    [Fact]
    public void Cascade_not_skipped_when_full_platform_utilization_enabled()
    {
        var benchmark = new AutonomousBenchmarkModeOptions
        {
            EnableBenchmarkMode = true,
            UseBenchmarkExecutionPath = true
        };
        var platform = new AutonomousPlatformUtilizationOptions
        {
            EnableFullPlatformUtilization = true
        };

        BenchmarkExecutionPathPolicy.UseDeterministicCascadeOnly(benchmark, platform).Should().BeFalse();
        BenchmarkExecutionPathPolicy.ShouldDeferFailedGate(
                benchmark,
                BenchmarkExecutionPathPolicy.Stages.Verify,
                platform)
            .Should().BeFalse();
    }
}
