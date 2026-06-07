using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunDiffAggregatorTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly Guid _runId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public RunDiffAggregatorTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"run-diff-{Guid.NewGuid():N}");
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
    public async Task ListAsync_AggregatesRolloutAndPatchAttempts()
    {
        WriteRollout();
        WritePatch();

        var aggregator = CreateAggregator();
        var list = await aggregator.ListAsync(_runId, new RunDiffQuery());

        list.Total.Should().Be(2);
        list.Items.Should().Contain(i => i.Path == "backend/app.py" && i.ToolName == "write_file");
        list.Items.Should().Contain(i => i.Path == "frontend/App.tsx" && i.ToolName == "apply_patch");
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsUnifiedPatch()
    {
        WritePatch();

        var aggregator = CreateAggregator();
        var detail = await aggregator.GetDetailAsync(_runId, "frontend/App.tsx");

        detail.Should().NotBeNull();
        detail!.Path.Should().Be("frontend/App.tsx");
        detail.UnifiedDiff.Should().Contain("--- a/App.tsx");
        detail.Provenance.Should().NotBeEmpty();
    }

    private RunDiffAggregator CreateAggregator()
    {
        var options = Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot });
        var checkpoints = new VerifyPassCheckpointService(
            options,
            NullLogger<VerifyPassCheckpointService>.Instance);
        return new RunDiffAggregator(
            options,
            checkpoints,
            NullLogger<RunDiffAggregator>.Instance);
    }

    private void WriteRollout()
    {
        var path = Path.Combine(_runsRoot, _runId.ToString("D"), "rollout.jsonl");
        var line = """
            {"type":"tool_use","stepNumber":3,"toolName":"write_file","inputJson":"{\"path\":\"backend/app.py\",\"content\":\"print(1)\"}","outputJson":"wrote backend/app.py","success":true,"timestamp":1710000000000}
            """;
        File.WriteAllText(path, line.Trim());
    }

    private void WritePatch()
    {
        var dir = Path.Combine(_runsRoot, _runId.ToString("D"), "patches");
        Directory.CreateDirectory(dir);
        var payload = """
            {
              "path": "frontend/App.tsx",
              "success": true,
              "patch": "--- a/App.tsx\\n+++ b/App.tsx\\n@@\\n-export default function App()\\n+export default function AppFixed()",
              "timestamp": "2026-01-01T00:00:00Z"
            }
            """;
        File.WriteAllText(Path.Combine(dir, "patch1.json"), payload);
    }
}
