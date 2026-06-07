using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraEvidenceStoreTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly FileSystemObscuraEvidenceStore _store;

    public ObscuraEvidenceStoreTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"obscura-evidence-{Guid.NewGuid():N}");
        _store = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task Persist_UsesContentAddressedFilename_AndManifest()
    {
        var runId = Guid.NewGuid();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var artifact = await _store.PersistAsync(
            runId,
            ObscuraEvidenceKind.Screenshot,
            png,
            new ObscuraEvidencePersistOptions(
                LogicalName: "screenshot-step1",
                StepNumber: 1,
                ToolName: "browser_screenshot",
                MirrorToVerifyFileNames: ["screenshot-final.png"]));

        artifact.FileName.Should().EndWith(".png");
        artifact.FileName.Should().NotContain("screenshot-step");
        artifact.ContentHash.Should().NotBeNullOrWhiteSpace();
        File.Exists(artifact.AbsolutePath).Should().BeTrue();
        File.Exists(Path.Combine(_store.GetVerifyDirectory(runId), "screenshot-final.png")).Should().BeTrue();

        var manifest = await _store.GetManifestAsync(runId);
        manifest.Artifacts.Should().ContainSingle();
        manifest.Artifacts[0].LogicalName.Should().Be("screenshot-step1");
    }

    [Fact]
    public void Dashboard_IncludesObscuraArtifacts()
    {
        var orchestrator = AppGenerationOrchestrator.Create("test", "fp");
        var runId = orchestrator.Id;
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03, 0x04 };
        _store.PersistAsync(
            runId,
            ObscuraEvidenceKind.Screenshot,
            png,
            new ObscuraEvidencePersistOptions(LogicalName: "shot", StepNumber: 2, ToolName: "browser_screenshot"))
            .GetAwaiter().GetResult();

        var verifyEvidence = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        var dashboard = new BuildDiagnosticsDashboardService(verifyEvidence, _store)
            .Build(orchestrator, new RunQualityAssessmentService().Assess(orchestrator));

        dashboard.ObscuraEvidence.Should().NotBeNull();
        dashboard.RunId.Should().Be(orchestrator.Id);
        dashboard.ObscuraEvidence!.Artifacts.Should().NotBeEmpty();
        dashboard.ObscuraEvidence.ManifestUrl.Should().Contain("manifest.json");
    }
}
