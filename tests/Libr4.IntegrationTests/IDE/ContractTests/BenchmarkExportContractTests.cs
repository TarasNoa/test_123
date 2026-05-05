using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IntegrationTests.IDE.ContractSamples;
using System.Text.Json;
using Xunit;

namespace Libr4.IntegrationTests.IDE.ContractTests;

/// <summary>
/// Integration-level contract tests for benchmark export DTO serialization shape.
/// These tests guard against accidental DTO breaking changes by validating the JSON structure.
/// </summary>
public class BenchmarkExportContractTests
{
    [Fact]
    public void BenchmarkDashboardExportDto_ShouldSerializeToExpectedShape()
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
            Runs: Array.Empty<BenchmarkRunPointDto>());

        var export = new BenchmarkDashboardExportDto(
            ExportId: "benchmark-dashboard-20260427220000",
            ContentSha256: "f1e2d3c4b5a6...",
            ArtifactPath: "d:/lib4_project/artifacts/benchmark-exports/benchmark-dashboard-20260427220000.json",
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            Dashboard: dashboard);

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate that the JSON contains expected fields
        json.Should().Contain("\"ExportId\"");
        json.Should().Contain("\"ContentSha256\"");
        json.Should().Contain("\"ArtifactPath\"");
        json.Should().Contain("\"GeneratedAtUtc\"");
        json.Should().Contain("\"Dashboard\"");
    }

    [Fact]
    public void BenchmarkDashboardExportDto_ShouldDeserializeFromSample()
    {
        var json = BenchmarkExportContractSamples.SuccessfulExport;
        
        var export = JsonSerializer.Deserialize<BenchmarkDashboardExportDto>(json);
        
        export.Should().NotBeNull();
        export!.ExportId.Should().Be("benchmark-dashboard-20260427220000");
        export.ContentSha256.Should().Be("f1e2d3c4b5a6...");
        export.ArtifactPath.Should().Contain("benchmark-exports");
        export.Dashboard.Should().NotBeNull();
        export.Dashboard.TotalRuns.Should().Be(25);
    }
}
