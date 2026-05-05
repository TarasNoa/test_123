using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IntegrationTests.IDE.ContractSamples;
using System.Text.Json;
using Xunit;

namespace Libr4.IntegrationTests.IDE.ContractTests;

/// <summary>
/// Integration-level contract tests for diagnostics bundle DTO serialization shape.
/// These tests guard against accidental DTO breaking changes by validating the JSON structure.
/// </summary>
public class DiagnosticsBundleContractTests
{
    [Fact]
    public void DiagnosticsBundleDto_ShouldSerializeToExpectedShape_ForHealthyDiagnostics()
    {
        var bundle = new DiagnosticsBundleDto(
            RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            BundleId: "diagnostics-00000000000000000000000000000001-20260427220000",
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            Manifest: new DiagnosticsManifestDto(
                Status: "Succeeded",
                FailureReason: null,
                IterationCount: 3,
                FileCount: 5,
                QualityGateCount: 3,
                BenchmarkSummary: new BenchmarkSummaryDto(
                    TotalQualityEvaluations: 25,
                    TotalFailedEvaluations: 2,
                    TotalCommandDurationMs: 45000,
                    AvgCommandDurationMs: 1800,
                    TopFailureReasons: Array.Empty<string>(),
                    Stages: Array.Empty<BenchmarkStageSummaryDto>()),
                McpLaneDiagnostics: Array.Empty<McpLaneDiagnosticsDto>(),
                McpLaneWatchdogSnapshot: new List<McpLaneWatchdogSnapshotDto>
                {
                    new McpLaneWatchdogSnapshotDto(
                        ProfileKey: "browser-lane",
                        Lane: "Browser",
                        LastCheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
                        Status: "available",
                        BlockerCode: null,
                        DiagnosticMessage: null,
                        History: new List<McpLaneWatchdogHistoryEntryDto>
                        {
                            new McpLaneWatchdogHistoryEntryDto(
                                CheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
                                Status: "available",
                                BlockerCode: null)
                        })
                }),
            Logs: new DiagnosticsLogsDto(
                SystemLogs: "=== System Logs ===\nStartedAt: 2026-04-27T21:00:00Z\nStatus: Succeeded",
                ApplicationLogs: "=== Application Logs ===\nIteration 1: Succeeded=true",
                ErrorLogs: "=== Error Logs ===\nNo errors"),
            Files: new DiagnosticsFilesDto(
                Files: new List<DiagnosticsFileEntryDto>
                {
                    new DiagnosticsFileEntryDto(
                        RelativePath: "src/Program.cs",
                        Language: "csharp",
                        SizeBytes: 1024,
                        Content: "using System;\n\nnamespace MyApp;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.WriteLine(\"Hello World\");\n    }\n}")
                }));

        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate that the JSON contains expected fields
        json.Should().Contain("\"RunId\"");
        json.Should().Contain("\"BundleId\"");
        json.Should().Contain("\"GeneratedAtUtc\"");
        json.Should().Contain("\"Manifest\"");
        json.Should().Contain("\"Logs\"");
        json.Should().Contain("\"Files\"");
    }

    [Fact]
    public void DiagnosticsBundleDto_ShouldSerializeToExpectedShape_ForDegradedMcpLane()
    {
        var bundle = new DiagnosticsBundleDto(
            RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            BundleId: "diagnostics-00000000000000000000000000000001-20260427220000",
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            Manifest: new DiagnosticsManifestDto(
                Status: "Failed",
                FailureReason: "Browser lane unavailable: mcp_server_missing",
                IterationCount: 5,
                FileCount: 3,
                QualityGateCount: 3,
                BenchmarkSummary: new BenchmarkSummaryDto(
                    TotalQualityEvaluations: 25,
                    TotalFailedEvaluations: 6,
                    TotalCommandDurationMs: 52000,
                    AvgCommandDurationMs: 2080,
                    TopFailureReasons: new List<string> { "mcp_server_missing" },
                    Stages: Array.Empty<BenchmarkStageSummaryDto>()),
                McpLaneDiagnostics: new List<McpLaneDiagnosticsDto>
                {
                    new McpLaneDiagnosticsDto(
                        Lane: "Browser",
                        DegradedEvents: 8,
                        TopBlockerCodes: new List<string> { "mcp_server_missing", "mcp_server_unreachable" })
                },
                McpLaneWatchdogSnapshot: new List<McpLaneWatchdogSnapshotDto>
                {
                    new McpLaneWatchdogSnapshotDto(
                        ProfileKey: "browser-lane",
                        Lane: "Browser",
                        LastCheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
                        Status: "degraded",
                        BlockerCode: "mcp_server_missing",
                        DiagnosticMessage: "MCP server executable not found: profile:browser-lane",
                        History: new List<McpLaneWatchdogHistoryEntryDto>
                        {
                            new McpLaneWatchdogHistoryEntryDto(
                                CheckTimeUtc: DateTime.Parse("2026-04-27T21:50:00Z").ToUniversalTime(),
                                Status: "degraded",
                                BlockerCode: "mcp_server_missing"),
                            new McpLaneWatchdogHistoryEntryDto(
                                CheckTimeUtc: DateTime.Parse("2026-04-27T21:55:00Z").ToUniversalTime(),
                                Status: "degraded",
                                BlockerCode: "mcp_server_missing"),
                            new McpLaneWatchdogHistoryEntryDto(
                                CheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
                                Status: "degraded",
                                BlockerCode: "mcp_server_missing")
                        })
                }),
            Logs: new DiagnosticsLogsDto(
                SystemLogs: "=== System Logs ===\nStartedAt: 2026-04-27T21:00:00Z\nStatus: Failed",
                ApplicationLogs: "=== Application Logs ===\nIteration 1: Succeeded=false",
                ErrorLogs: "=== Error Logs ===\nFailureReason: Browser lane unavailable"),
            Files: new DiagnosticsFilesDto(
                Files: new List<DiagnosticsFileEntryDto>
                {
                    new DiagnosticsFileEntryDto(
                        RelativePath: "src/Program.cs",
                        Language: "csharp",
                        SizeBytes: 512,
                        Content: "using System;\n\nnamespace MyApp;\n\nclass Program\n{\n    static void Main()\n    {\n        // Incomplete due to MCP failure\n    }\n}")
                }));

        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate degraded MCP lane fields
        json.Should().Contain("\"Status\": \"Failed\"");
        json.Should().Contain("\"McpLaneDiagnostics\"");
        json.Should().Contain("\"DegradedEvents\": 8");
        json.Should().Contain("\"mcp_server_missing\"");
        json.Should().Contain("\"History\"");
        json.Should().Contain("\"CheckTimeUtc\"");
    }

    [Fact]
    public void McpLaneWatchdogSnapshotDto_ShouldSerializeToExpectedShape_WithHistory()
    {
        var snapshot = new McpLaneWatchdogSnapshotDto(
            ProfileKey: "browser-lane",
            Lane: "Browser",
            LastCheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
            Status: "degraded",
            BlockerCode: "mcp_server_missing",
            DiagnosticMessage: "MCP server executable not found",
            History: new List<McpLaneWatchdogHistoryEntryDto>
            {
                new McpLaneWatchdogHistoryEntryDto(
                    CheckTimeUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime(),
                    Status: "degraded",
                    BlockerCode: "mcp_server_missing")
            });

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        
        json.Should().Contain("\"ProfileKey\"");
        json.Should().Contain("\"Lane\"");
        json.Should().Contain("\"LastCheckTimeUtc\"");
        json.Should().Contain("\"Status\"");
        json.Should().Contain("\"BlockerCode\"");
        json.Should().Contain("\"DiagnosticMessage\"");
        json.Should().Contain("\"History\"");
    }

    [Fact]
    public void DiagnosticsBundleDto_ShouldDeserializeFromSample_ForHealthyDiagnostics()
    {
        var json = DiagnosticsBundleContractSamples.HealthyDiagnostics;
        
        var bundle = JsonSerializer.Deserialize<DiagnosticsBundleDto>(json);
        
        bundle.Should().NotBeNull();
        bundle!.Manifest.Status.Should().Be("Succeeded");
        bundle.Manifest.McpLaneDiagnostics.Should().BeEmpty();
        bundle.Manifest.McpLaneWatchdogSnapshot.Should().NotBeEmpty();
        bundle.Manifest.McpLaneWatchdogSnapshot.First().Status.Should().Be("available");
    }

    [Fact]
    public void DiagnosticsBundleDto_ShouldDeserializeFromSample_ForDegradedMcpLane()
    {
        var json = DiagnosticsBundleContractSamples.DegradedMcpLaneDiagnostics;
        
        var bundle = JsonSerializer.Deserialize<DiagnosticsBundleDto>(json);
        
        bundle.Should().NotBeNull();
        bundle!.Manifest.Status.Should().Be("Failed");
        bundle.Manifest.McpLaneDiagnostics.Should().NotBeEmpty();
        bundle.Manifest.McpLaneDiagnostics.First().DegradedEvents.Should().Be(8);
        bundle.Manifest.McpLaneWatchdogSnapshot.Should().NotBeEmpty();
        bundle.Manifest.McpLaneWatchdogSnapshot.First().Status.Should().Be("degraded");
        bundle.Manifest.McpLaneWatchdogSnapshot.First().History.Should().NotBeEmpty();
    }
}
