using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunProviderCostTrackerTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly RunProviderCostTracker _tracker;

    public RunProviderCostTrackerTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), "libr4-cost-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_runsRoot);
        _tracker = new RunProviderCostTracker(Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }));
    }

    [Fact]
    public async Task RecordAsync_PersistsAndRollsUpByProvider()
    {
        var runId = Guid.NewGuid();

        await _tracker.RecordAsync(runId, "openrouter", "planning", "claude-3.5", 1200, 0.05m);
        await _tracker.RecordAsync(runId, "openrouter", "generation", "gpt-4o", 2400, 0.10m);
        await _tracker.RecordAsync(runId, "dockermodelrunner", "fixing", "coder-9b", 800, 0m);

        var entries = _tracker.GetEntries(runId);
        entries.Should().HaveCount(3);

        var rollup = _tracker.RollupByProvider(runId);
        rollup.Should().ContainKey("openrouter");
        rollup["openrouter"].TotalTokens.Should().Be(3600);
        rollup["openrouter"].TotalCostUsd.Should().Be(0.15m);
        rollup["openrouter"].RequestCount.Should().Be(2);
        rollup["dockermodelrunner"].TotalTokens.Should().Be(800);
    }

    [Fact]
    public async Task GetEntries_ReloadsFromDiskWhenMemoryEmpty()
    {
        var runId = Guid.NewGuid();
        await _tracker.RecordAsync(runId, "openrouter", "planning", "model-a", 500, 0.01m);

        var freshTracker = new RunProviderCostTracker(Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }));
        var entries = freshTracker.GetEntries(runId);

        entries.Should().HaveCount(1);
        entries[0].ProviderId.Should().Be("openrouter");
        entries[0].Tokens.Should().Be(500);
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
            // best-effort cleanup
        }
    }
}
