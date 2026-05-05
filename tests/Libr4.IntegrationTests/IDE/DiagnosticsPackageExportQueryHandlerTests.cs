using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DiagnosticsPackageExportQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPersistZippedDiagnosticsArtifact()
    {
        var runId = Guid.NewGuid();
        var fakeBundle = new DiagnosticsBundleDto(
            RunId: runId,
            BundleId: "bundle-test",
            GeneratedAtUtc: DateTime.UtcNow,
            Manifest: new DiagnosticsManifestDto(
                Status: "Completed",
                FailureReason: null,
                IterationCount: 1,
                FileCount: 1,
                QualityGateCount: 1,
                BenchmarkSummary: new BenchmarkSummaryDto(
                    TotalQualityEvaluations: 1,
                    TotalFailedEvaluations: 0,
                    TotalCommandDurationMs: 100,
                    AvgCommandDurationMs: 100,
                    TopFailureReasons: Array.Empty<string>(),
                    Stages: Array.Empty<BenchmarkStageSummaryDto>()),
                McpLaneDiagnostics: Array.Empty<McpLaneDiagnosticsDto>(),
                McpLaneWatchdogSnapshot: Array.Empty<McpLaneWatchdogSnapshotDto>()),
            Logs: new DiagnosticsLogsDto("sys", "app", "err"),
            Files: new DiagnosticsFilesDto(new[]
            {
                new DiagnosticsFileEntryDto("app.py", "python", 12, "print('ok')")
            }));

        var exportDir = Path.Combine(Path.GetTempPath(), $"libr4-diag-test-{Guid.NewGuid():N}");
        var handler = new ExportDiagnosticsPackageQueryHandler(
            new FakeDiagnosticsBundleService(fakeBundle),
            Options.Create(new DiagnosticsExportOptions
            {
                ExportRootPath = exportDir,
                RetentionHours = 1,
                MaxArtifacts = 20
            }));

        var result = await handler.Handle(new ExportDiagnosticsPackageQuery(runId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.RunId.Should().Be(runId);
        result.ArtifactPath.Should().EndWith(".zip");
        File.Exists(result.ArtifactPath).Should().BeTrue();
        result.ContentSha256.Should().HaveLength(64);
    }

    private sealed class FakeDiagnosticsBundleService : IDiagnosticsBundleService
    {
        private readonly DiagnosticsBundleDto _bundle;

        public FakeDiagnosticsBundleService(DiagnosticsBundleDto bundle) => _bundle = bundle;

        public Task<DiagnosticsBundleDto?> GenerateBundleAsync(Guid orchestratorId, CancellationToken ct = default)
            => Task.FromResult<DiagnosticsBundleDto?>(_bundle);
    }
}
