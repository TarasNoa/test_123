using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunUsageRollupServiceTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly RunUsageRollupService _service;

    public RunUsageRollupServiceTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"usage-rollup-{Guid.NewGuid():N}");
        _service = new RunUsageRollupService(Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }));
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
    public void Rollup_AggregatesStepsToolsAndUsage()
    {
        var runId = Guid.NewGuid();
        var dir = Path.Combine(_runsRoot, runId.ToString("D"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "rollout.jsonl");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllLines(path,
        [
            $$"""{"type":"step_start","sessionId":"s1","stepNumber":1,"timestamp":{{ts}}}""",
            $$"""{"type":"tool_use","sessionId":"s1","stepNumber":1,"toolName":"read_file","timestamp":{{ts + 100}}}""",
            $$"""{"type":"step_finish","sessionId":"s1","stepNumber":1,"finishReason":"tool_calls","usage":{"inputTokens":120,"outputTokens":80,"totalTokens":200,"costUsd":0.0025},"timestamp":{{ts + 200}}}"""
        ]);

        var rollup = _service.Rollup(runId);

        rollup.StepCount.Should().Be(2);
        rollup.ToolCallCount.Should().Be(1);
        rollup.InputTokens.Should().Be(120);
        rollup.OutputTokens.Should().Be(80);
        rollup.TotalTokens.Should().Be(200);
        rollup.CostUsd.Should().BeApproximately(0.0025, 0.0001);
        rollup.LastToolActivityAtUtc.Should().NotBeNull();
    }
}
