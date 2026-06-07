using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class EvidenceDiffCorrelatorTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly Guid _runId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public EvidenceDiffCorrelatorTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"evidence-diff-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_runsRoot, _runId.ToString("D")));
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
    public async Task GetOverlaysAsync_FlagsConsoleStackAndSecurityPaths()
    {
        var repo = new InMemoryAppGenerationRepository();
        var orchestrator = AppGenerationOrchestrator.Create("test", "fp");
        orchestrator.RecordSecurityReview(new SecurityReviewAuditEntry(
            "post_generation",
            4,
            false,
            ["private_key_material:backend/secrets.pem"],
            ["Remove embedded key"],
            DateTime.UtcNow));
        await repo.SaveAsync(orchestrator);

        var runId = orchestrator.Id;
        WriteConsoleErrorsForRun(
            runId,
            """[{"level":"error","message":"TypeError at frontend/App.tsx:12:5 — undefined is not a function"}]""");

        var correlator = CreateCorrelator(repo);
        var overlays = await correlator.GetOverlaysAsync(runId);

        overlays.Paths.Should().Contain(p => p.Path == "frontend/App.tsx");
        overlays.Paths.Should().Contain(p => p.Path == "backend/secrets.pem");
        overlays.Paths.Single(p => p.Path == "frontend/App.tsx").OverlayKinds
            .Should().Contain("verify_console");
        overlays.Paths.Single(p => p.Path == "backend/secrets.pem").OverlayKinds
            .Should().Contain("security_flag");
    }

    [Fact]
    public async Task GetForPathAsync_CorrelatesObscuraByStepNumber()
    {
        var runId = _runId;
        WriteRollout(runId, stepNumber: 5);
        await WriteObscuraScreenshotAsync(runId, stepNumber: 5);
        WriteConsoleErrorsForRun(runId, "[]");

        var correlator = CreateCorrelator();
        var evidence = await correlator.GetForPathAsync(runId, "src/App.tsx");

        evidence.Should().NotBeNull();
        evidence!.CorrelatedStepNumber.Should().Be(5);
        evidence.Items.Should().Contain(i =>
            i.Source == "obscura"
            && i.StepMatched
            && i.StepNumber == 5);
    }

    [Fact]
    public void ParseConsoleErrorPaths_ExtractsStackPaths()
    {
        var json = """
            [{"message":"Error in frontend/App.tsx:42:10\n    at render (frontend/App.tsx:42:10)"}]
            """;

        var paths = EvidenceDiffCorrelator.ParseConsoleErrorPaths(json);

        paths.Should().Contain("frontend/App.tsx");
    }

    [Fact]
    public void TryExtractPathFromSecurityReason_ParsesCategoryPath()
    {
        EvidenceDiffCorrelator.TryExtractPathFromSecurityReason(
            "private_key_material:backend/secrets.pem",
            out var path).Should().BeTrue();
        path.Should().Be("backend/secrets.pem");
    }

    private RunDiffAggregator CreateDiffAggregator()
    {
        var options = Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot });
        return new RunDiffAggregator(
            options,
            new VerifyPassCheckpointService(options, NullLogger<VerifyPassCheckpointService>.Instance),
            NullLogger<RunDiffAggregator>.Instance);
    }

    private EvidenceDiffCorrelator CreateCorrelator(InMemoryAppGenerationRepository? repo = null)
    {
        var diffAggregator = CreateDiffAggregator();

        var obscura = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);

        var verify = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);

        return new EvidenceDiffCorrelator(
            diffAggregator,
            NullLogger<EvidenceDiffCorrelator>.Instance,
            obscura,
            verify,
            repo);
    }

    private void WriteRollout(Guid runId, int stepNumber)
    {
        var path = Path.Combine(_runsRoot, runId.ToString("D"), "rollout.jsonl");
        var line = $$"""
            {"type":"tool_use","stepNumber":{{stepNumber}},"toolName":"write_file","inputJson":"{\"path\":\"src/App.tsx\",\"content\":\"export {}\"}","outputJson":"wrote src/App.tsx","success":true,"timestamp":1710000000000}
            """;
        File.WriteAllText(path, line.Trim());
    }

    private async Task WriteObscuraScreenshotAsync(Guid runId, int stepNumber)
    {
        var store = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);

        await store.PersistAsync(
            runId,
            ObscuraEvidenceKind.Screenshot,
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            new ObscuraEvidencePersistOptions(
                LogicalName: "screenshot-step",
                StepNumber: stepNumber,
                ToolName: "browser_screenshot"));
    }


    private void WriteConsoleErrorsForRun(Guid runId, string json)
    {
        var dir = Path.Combine(_runsRoot, runId.ToString("D"), "verify");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "console-errors.json"), json);
    }
}
