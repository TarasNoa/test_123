using System.Text;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifyEvidenceStoreTests : IDisposable
{
    private readonly string _evidenceRoot;
    private readonly FileSystemVerifyEvidenceStore _store;

    public VerifyEvidenceStoreTests()
    {
        _evidenceRoot = Path.Combine(Path.GetTempPath(), $"verify-evidence-{Guid.NewGuid():N}");
        _store = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _evidenceRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_evidenceRoot))
                Directory.Delete(_evidenceRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task PersistAsync_WritesKnownArtifacts()
    {
        var runId = Guid.NewGuid();
        await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"passed":true}""")))
        {
            await _store.PersistAsync(runId, VerifyEvidenceKind.VerifyReport, stream);
        }

        var bundle = _store.List(runId);
        bundle.DirectoryExists.Should().BeTrue();
        bundle.Artifacts.Should().ContainSingle(a => a.Kind == VerifyEvidenceKind.VerifyReport);
        bundle.Artifacts[0].DownloadUrl.Should().Contain($"/api/ide/app-generation/{runId:D}/verify/artifacts/");
    }

    [Fact]
    public void List_ReturnsCanonicalArtifactKinds()
    {
        var runId = Guid.NewGuid();
        var dir = _store.GetEvidenceDirectory(runId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "app.log"), "boot ok");
        File.WriteAllText(Path.Combine(dir, "readiness.json"), """{"ready":true}""");
        File.WriteAllText(Path.Combine(dir, "verify-report.json"), """{"passed":true}""");
        File.WriteAllText(Path.Combine(dir, "manifest.json"), """{"recipeId":"django"}""");

        var bundle = _store.List(runId);
        bundle.Artifacts.Select(a => a.Kind).Should().Contain(VerifyEvidenceKind.AppLog);
        bundle.Artifacts.Select(a => a.Kind).Should().Contain(VerifyEvidenceKind.Readiness);
        bundle.Artifacts.Select(a => a.Kind).Should().Contain(VerifyEvidenceKind.VerifyReport);
        bundle.Artifacts.Select(a => a.Kind).Should().Contain(VerifyEvidenceKind.Manifest);
    }

    [Fact]
    public void TryGet_RejectsPathTraversal()
    {
        var runId = Guid.NewGuid();
        _store.TryGet(runId, "../secret.txt").Should().BeNull();
    }

    [Fact]
    public void List_ExposesScreenshotThumbnailUrl()
    {
        var runId = Guid.NewGuid();
        var dir = _store.GetEvidenceDirectory(runId);
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "screenshot-final.png"), [0x89, 0x50, 0x4E, 0x47]);

        var bundle = _store.List(runId);
        bundle.ThumbnailUrl.Should().NotBeNullOrWhiteSpace();
        bundle.Artifacts.Single(a => a.Kind == VerifyEvidenceKind.Screenshot).ThumbnailUrl
            .Should().Be(bundle.ThumbnailUrl);
    }

    [Fact]
    public void Dashboard_IncludesVerifyArtifacts()
    {
        var orchestrator = AppGenerationOrchestrator.Create("Calorie app", "fp-verify-dashboard");
        var orchestratorDir = _store.GetEvidenceDirectory(orchestrator.Id);
        Directory.CreateDirectory(orchestratorDir);
        File.WriteAllText(Path.Combine(orchestratorDir, "verify-report.json"), """{"passed":false}""");
        File.WriteAllText(Path.Combine(orchestratorDir, "verify-failure-evidence.json"), """{"repairHint":"readiness failed"}""");

        var dashboard = new BuildDiagnosticsDashboardService(_store)
            .Build(orchestrator, new RunQualityAssessmentService().Assess(orchestrator));

        dashboard.VerifyEvidence.Should().NotBeNull();
        dashboard.VerifyEvidence!.Artifacts.Should().HaveCountGreaterOrEqualTo(2);
        dashboard.VerifyEvidence.ThumbnailUrl.Should().BeNull();
    }
}
