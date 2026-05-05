using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IntegrationTests.IDE.ContractSamples;
using System.Text.Json;
using Xunit;

namespace Libr4.IntegrationTests.IDE.ContractTests;

/// <summary>
/// Integration-level contract tests for benchmark dashboard DTO serialization shape.
/// These tests guard against accidental DTO breaking changes by validating the JSON structure.
/// </summary>
public class BenchmarkDashboardContractTests
{
    [Fact]
    public void BenchmarkDashboardDto_ShouldSerializeToExpectedShape_ForHealthyRunTrend()
    {
        var dashboard = new BenchmarkDashboardDto(
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            TotalRuns: 25,
            SucceededRuns: 23,
            FailedRuns: 2,
            SuccessRate: 0.92,
            TotalMcpDegradedEvents: 0,
            TopMcpBlockerCodes: Array.Empty<string>(),
            TopFailureReasons: Array.Empty<string>(),
            StageTrends: new List<BenchmarkStageTrendDto>
            {
                new BenchmarkStageTrendDto(
                    Stage: "Build",
                    Evaluations: 25,
                    AverageScore: 9.5,
                    PassRate: 0.95,
                    AverageDurationMs: 12000)
            },
            TopRegressions: Array.Empty<BenchmarkRegressionDto>(),
            Runs: new List<BenchmarkRunPointDto>
            {
                new BenchmarkRunPointDto(
                    RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Status: "Succeeded",
                    StartedAtUtc: DateTime.Parse("2026-04-27T21:00:00Z").ToUniversalTime(),
                    CompletedAtUtc: DateTime.Parse("2026-04-27T21:00:42Z").ToUniversalTime(),
                    OverallScore: 9,
                    FailedQualityGates: 0,
                    TotalCommandDurationMs: 42000)
            });

        var json = JsonSerializer.Serialize(dashboard, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate that the JSON contains expected fields
        json.Should().Contain("\"GeneratedAtUtc\"");
        json.Should().Contain("\"TotalRuns\"");
        json.Should().Contain("\"SuccessRate\"");
        json.Should().Contain("\"TotalMcpDegradedEvents\"");
        json.Should().Contain("\"TopMcpBlockerCodes\"");
        json.Should().Contain("\"TopFailureReasons\"");
        json.Should().Contain("\"StageTrends\"");
        json.Should().Contain("\"TopRegressions\"");
        json.Should().Contain("\"Runs\"");
    }

    [Fact]
    public void BenchmarkDashboardDto_ShouldSerializeToExpectedShape_ForDegradedMcpLane()
    {
        var dashboard = new BenchmarkDashboardDto(
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            TotalRuns: 25,
            SucceededRuns: 19,
            FailedRuns: 6,
            SuccessRate: 0.76,
            TotalMcpDegradedEvents: 12,
            TopMcpBlockerCodes: new List<string> { "mcp_server_missing", "mcp_server_unreachable" },
            TopFailureReasons: new List<string> { "Browser lane unavailable", "n8n workflow timeout" },
            StageTrends: new List<BenchmarkStageTrendDto>
            {
                new BenchmarkStageTrendDto(
                    Stage: "Build",
                    Evaluations: 25,
                    AverageScore: 8.5,
                    PassRate: 0.80,
                    AverageDurationMs: 15000)
            },
            TopRegressions: Array.Empty<BenchmarkRegressionDto>(),
            Runs: new List<BenchmarkRunPointDto>
            {
                new BenchmarkRunPointDto(
                    RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Status: "Failed",
                    StartedAtUtc: DateTime.Parse("2026-04-27T21:00:00Z").ToUniversalTime(),
                    CompletedAtUtc: DateTime.Parse("2026-04-27T21:00:58Z").ToUniversalTime(),
                    OverallScore: 7,
                    FailedQualityGates: 2,
                    TotalCommandDurationMs: 58000)
            });

        var json = JsonSerializer.Serialize(dashboard, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate degraded MCP lane fields
        json.Should().Contain("\"TotalMcpDegradedEvents\": 12");
        json.Should().Contain("\"mcp_server_missing\"");
        json.Should().Contain("\"mcp_server_unreachable\"");
    }

    [Fact]
    public void BenchmarkDashboardDto_ShouldSerializeToExpectedShape_ForRegressionHeavy()
    {
        var dashboard = new BenchmarkDashboardDto(
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            TotalRuns: 25,
            SucceededRuns: 17,
            FailedRuns: 8,
            SuccessRate: 0.68,
            TotalMcpDegradedEvents: 3,
            TopMcpBlockerCodes: new List<string> { "mcp_server_unavailable" },
            TopFailureReasons: new List<string> { "Quality gate failed", "Test execution timeout" },
            StageTrends: new List<BenchmarkStageTrendDto>
            {
                new BenchmarkStageTrendDto(
                    Stage: "Build",
                    Evaluations: 25,
                    AverageScore: 7.2,
                    PassRate: 0.65,
                    AverageDurationMs: 14000)
            },
            TopRegressions: new List<BenchmarkRegressionDto>
            {
                new BenchmarkRegressionDto(
                    Stage: "Build",
                    BaselineAverageScore: 9.5,
                    LatestScore: 7,
                    Delta: -2.5,
                    LatestFailureReasons: new List<string> 
                    { 
                        "Build stage score dropped below threshold",
                        "Compilation errors in generated code" 
                    })
            },
            Runs: new List<BenchmarkRunPointDto>
            {
                new BenchmarkRunPointDto(
                    RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Status: "Failed",
                    StartedAtUtc: DateTime.Parse("2026-04-27T21:00:00Z").ToUniversalTime(),
                    CompletedAtUtc: DateTime.Parse("2026-04-27T21:00:55Z").ToUniversalTime(),
                    OverallScore: 6,
                    FailedQualityGates: 3,
                    TotalCommandDurationMs: 55000)
            });

        var json = JsonSerializer.Serialize(dashboard, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate regression fields
        json.Should().Contain("\"TopRegressions\"");
        json.Should().Contain("\"Stage\"");
        json.Should().Contain("\"BaselineAverageScore\"");
        json.Should().Contain("\"LatestScore\"");
        json.Should().Contain("\"Delta\"");
        json.Should().Contain("\"LatestFailureReasons\"");
        json.Should().Contain("-2.5");
    }

    [Fact]
    public void BenchmarkRegressionDto_ShouldSerializeToExpectedShape()
    {
        var regression = new BenchmarkRegressionDto(
            Stage: "Build",
            BaselineAverageScore: 9.5,
            LatestScore: 7,
            Delta: -2.5,
            LatestFailureReasons: new List<string> { "Build stage score dropped below threshold" });

        var json = JsonSerializer.Serialize(regression, new JsonSerializerOptions { WriteIndented = true });
        
        json.Should().Contain("\"Stage\"");
        json.Should().Contain("\"BaselineAverageScore\"");
        json.Should().Contain("\"LatestScore\"");
        json.Should().Contain("\"Delta\"");
        json.Should().Contain("\"LatestFailureReasons\"");
    }

    [Fact]
    public void BenchmarkDashboardDto_ShouldDeserializeFromSample_ForHealthyRunTrend()
    {
        var json = BenchmarkDashboardContractSamples.HealthyRunTrend;
        
        var dashboard = JsonSerializer.Deserialize<BenchmarkDashboardDto>(json);
        
        dashboard.Should().NotBeNull();
        dashboard!.TotalRuns.Should().Be(25);
        dashboard.SuccessRate.Should().Be(0.92);
        dashboard.TotalMcpDegradedEvents.Should().Be(0);
        dashboard.TopRegressions.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkDashboardDto_ShouldDeserializeFromSample_ForDegradedMcpLane()
    {
        var json = BenchmarkDashboardContractSamples.DegradedMcpLane;
        
        var dashboard = JsonSerializer.Deserialize<BenchmarkDashboardDto>(json);
        
        dashboard.Should().NotBeNull();
        dashboard!.TotalMcpDegradedEvents.Should().Be(12);
        dashboard.TopMcpBlockerCodes.Should().Contain("mcp_server_missing");
        dashboard.TopFailureReasons.Should().Contain("Browser lane unavailable");
    }

    [Fact]
    public void BenchmarkDashboardDto_ShouldDeserializeFromSample_ForRegressionHeavy()
    {
        var json = BenchmarkDashboardContractSamples.RegressionHeavy;
        
        var dashboard = JsonSerializer.Deserialize<BenchmarkDashboardDto>(json);
        
        dashboard.Should().NotBeNull();
        dashboard!.TopRegressions.Should().NotBeEmpty();
        dashboard.TopRegressions.First().Stage.Should().Be("Build");
        dashboard.TopRegressions.First().Delta.Should().Be(-2.5);
        dashboard.TopRegressions.First().LatestFailureReasons.Should().NotBeEmpty();
    }
}
