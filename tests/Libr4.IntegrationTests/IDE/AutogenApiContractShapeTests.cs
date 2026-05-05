using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutogenApiContractShapeTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void BenchmarkDashboardDto_ShouldContainUiReadyFields()
    {
        var dto = new BenchmarkDashboardDto(
            GeneratedAtUtc: DateTime.UtcNow,
            TotalRuns: 2,
            SucceededRuns: 1,
            FailedRuns: 1,
            SuccessRate: 0.5,
            TotalMcpDegradedEvents: 1,
            TopMcpBlockerCodes: new[] { "mcp_server_missing" },
            TopFailureReasons: new[] { "build_failed" },
            StageTrends: new[]
            {
                new BenchmarkStageTrendDto("build", 2, 6.5, 0.5, 1000)
            },
            TopRegressions: new[]
            {
                new BenchmarkRegressionDto("build", 8.0, 4, -4.0, new[] { "build_failed" })
            },
            Runs: new[]
            {
                new BenchmarkRunPointDto(Guid.NewGuid(), "Failed", DateTime.UtcNow, DateTime.UtcNow, 6, 1, 1200)
            });

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto, WebJson));
        var root = doc.RootElement;
        root.TryGetProperty("topRegressions", out _).Should().BeTrue();
        root.TryGetProperty("topMcpBlockerCodes", out _).Should().BeTrue();
        root.TryGetProperty("stageTrends", out _).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticsBundleDto_ShouldContainWatchdogAndLaneDiagnostics()
    {
        var dto = new DiagnosticsBundleDto(
            RunId: Guid.NewGuid(),
            BundleId: "bundle",
            GeneratedAtUtc: DateTime.UtcNow,
            Manifest: new DiagnosticsManifestDto(
                Status: "Completed",
                FailureReason: null,
                IterationCount: 1,
                FileCount: 1,
                QualityGateCount: 1,
                BenchmarkSummary: new BenchmarkSummaryDto(1, 0, 100, 100, Array.Empty<string>(), Array.Empty<BenchmarkStageSummaryDto>()),
                McpLaneDiagnostics: new[] { new McpLaneDiagnosticsDto("Browser", 1, new[] { "mcp_server_missing" }) },
                McpLaneWatchdogSnapshot: new[]
                {
                    new McpLaneWatchdogSnapshotDto("browser-lane", "Browser", DateTime.UtcNow, "degraded", "mcp_server_missing", "missing", Array.Empty<McpLaneWatchdogHistoryEntryDto>())
                }),
            Logs: new DiagnosticsLogsDto("sys", "app", "err"),
            Files: new DiagnosticsFilesDto(new[] { new DiagnosticsFileEntryDto("a.txt", "text", 1, "x") }));

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto, WebJson));
        var manifest = doc.RootElement.GetProperty("manifest");
        manifest.TryGetProperty("mcpLaneDiagnostics", out _).Should().BeTrue();
        manifest.TryGetProperty("mcpLaneWatchdogSnapshot", out _).Should().BeTrue();
    }
}
