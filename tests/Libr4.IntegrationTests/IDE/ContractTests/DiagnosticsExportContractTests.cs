using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IntegrationTests.IDE.ContractSamples;
using System.Text.Json;
using Xunit;

namespace Libr4.IntegrationTests.IDE.ContractTests;

/// <summary>
/// Integration-level contract tests for diagnostics export DTO serialization shape.
/// These tests guard against accidental DTO breaking changes by validating the JSON structure.
/// </summary>
public class DiagnosticsExportContractTests
{
    [Fact]
    public void DiagnosticsPackageExportDto_ShouldSerializeToExpectedShape()
    {
        var export = new DiagnosticsPackageExportDto(
            RunId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ExportId: "diagnostics-00000000000000000000000000000001-20260427220000",
            ContentSha256: "a1b2c3d4e5f6...",
            ArtifactPath: "d:/lib4_project/artifacts/diagnostics-exports/diagnostics-00000000000000000000000000000001-20260427220000.zip",
            GeneratedAtUtc: DateTime.Parse("2026-04-27T22:00:00Z").ToUniversalTime());

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        
        // Validate that the JSON contains expected fields
        json.Should().Contain("\"RunId\"");
        json.Should().Contain("\"ExportId\"");
        json.Should().Contain("\"ContentSha256\"");
        json.Should().Contain("\"ArtifactPath\"");
        json.Should().Contain("\"GeneratedAtUtc\"");
    }

    [Fact]
    public void DiagnosticsPackageExportDto_ShouldDeserializeFromSample()
    {
        var json = DiagnosticsExportContractSamples.SuccessfulExport;
        
        var export = JsonSerializer.Deserialize<DiagnosticsPackageExportDto>(json);
        
        export.Should().NotBeNull();
        export!.RunId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        export.ExportId.Should().Be("diagnostics-00000000000000000000000000000001-20260427220000");
        export.ContentSha256.Should().Be("a1b2c3d4e5f6...");
        export.ArtifactPath.Should().Contain("diagnostics-exports");
    }
}
