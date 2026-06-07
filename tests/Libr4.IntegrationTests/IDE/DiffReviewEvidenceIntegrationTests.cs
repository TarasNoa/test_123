using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>Phase 7.4.5 — verify fail evidence correlation for review UI.</summary>
public sealed class DiffReviewEvidenceIntegrationTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly Guid _runId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public DiffReviewEvidenceIntegrationTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"diff-review-evidence-{Guid.NewGuid():N}");
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
    public async Task VerifyFail_EvidenceForPath_IncludesConsoleAndScreenshotForSameStep()
    {
        const int step = 7;
        WriteRollout(_runId, step);
        WriteConsoleErrors(
            """[{"level":"error","message":"TypeError at frontend/App.tsx:12:5 — render failed"}]""");
        await WriteVerifyScreenshotAsync(_runId);
        await WriteObscuraScreenshotAsync(_runId, step);

        var correlator = CreateCorrelator();
        var evidence = await correlator.GetForPathAsync(_runId, "frontend/App.tsx");

        evidence.Should().NotBeNull();
        evidence!.CorrelatedStepNumber.Should().Be(step);
        evidence.Items.Should().Contain(i =>
            i.Source == "verify"
            && i.Kind.Contains("Screenshot", StringComparison.OrdinalIgnoreCase));
        evidence.Items.Should().Contain(i =>
            i.Source == "obscura"
            && i.StepMatched
            && i.StepNumber == step);
        evidence.Overlays.Should().Contain(o =>
            o.Kind == "verify_console"
            && o.Reason.Contains("frontend/App.tsx", StringComparison.OrdinalIgnoreCase));
    }

    private EvidenceDiffCorrelator CreateCorrelator() =>
        new(
            CreateDiffAggregator(),
            NullLogger<EvidenceDiffCorrelator>.Instance,
            new FileSystemObscuraEvidenceStore(
                Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
                NullLogger<FileSystemObscuraEvidenceStore>.Instance),
            new FileSystemVerifyEvidenceStore(
                Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
                NullLogger<FileSystemVerifyEvidenceStore>.Instance));

    private RunDiffAggregator CreateDiffAggregator()
    {
        var options = Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot });
        return new RunDiffAggregator(
            options,
            new VerifyPassCheckpointService(options, NullLogger<VerifyPassCheckpointService>.Instance),
            NullLogger<RunDiffAggregator>.Instance);
    }

    private void WriteRollout(Guid runId, int stepNumber)
    {
        var path = Path.Combine(_runsRoot, runId.ToString("D"), "rollout.jsonl");
        var line = $$"""
            {"type":"tool_use","stepNumber":{{stepNumber}},"toolName":"write_file","inputJson":"{\"path\":\"frontend/App.tsx\",\"content\":\"export {}\"}","outputJson":"wrote frontend/App.tsx","success":true,"timestamp":1710000000000}
            """;
        File.WriteAllText(path, line.Trim());
    }

    private void WriteConsoleErrors(string json)
    {
        var dir = Path.Combine(_runsRoot, _runId.ToString("D"), "verify");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "console-errors.json"), json);
    }

    private async Task WriteVerifyScreenshotAsync(Guid runId)
    {
        var store = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        await store.PersistAsync(
            runId,
            VerifyEvidenceKind.Screenshot,
            new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
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
                LogicalName: "verify-step",
                StepNumber: stepNumber,
                ToolName: "browser_screenshot"));
    }
}
