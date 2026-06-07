using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SessionTimelineServiceTests : IDisposable
{
    private readonly string _root;
    private readonly Guid _runId = Guid.NewGuid();

    public SessionTimelineServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"session-timeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task GetTimeline_AggregatesRolloutSubagentsDelegationsAndVerify()
    {
        var runDir = Path.Combine(_root, _runId.ToString("D"));
        Directory.CreateDirectory(runDir);

        var rolloutPath = Path.Combine(runDir, "rollout.jsonl");
        await File.WriteAllTextAsync(rolloutPath, """
            {"type":"tool_use","sessionId":"main","stepNumber":1,"toolName":"read_file","inputJson":"{}","outputJson":"{\"ok\":true}","success":true,"timing":{"durationMs":12},"timestamp":1700000000000}
            {"type":"permission","toolName":"shell","decision":"allow","reason":"policy","timestamp":1700000001000}
            """);

        var verifyDir = Path.Combine(runDir, "verify");
        Directory.CreateDirectory(verifyDir);
        await File.WriteAllTextAsync(Path.Combine(verifyDir, "readiness.json"), """{"ready":true}""");

        var subagents = new Mock<ISubagentStore>();
        subagents.Setup(x => x.ListAsync(_runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SubagentRecord("sub1", _runId, "verifier", "check ui", "completed",
                    DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(-4), "done", null)
            });

        var delegations = new Mock<IDelegationManager>();
        delegations.Setup(x => x.ListAsync(_runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new DelegationRecord("del-1", _runId, "explore repo", "completed",
                    DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow.AddMinutes(-2), "found 3 files", null, false)
            });

        var flow = new Mock<IFlowProgressStore>();
        flow.Setup(x => x.LoadAsync(_runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlowProgress(
                _runId,
                "calorie",
                "verify",
                "running",
                new[] { new FlowNodeProgress("verify", "completed", 1, null) },
                DateTime.UtcNow));

        var service = new SessionTimelineService(
            Options.Create(new AgentFleetOptions { RunsRoot = _root }),
            flow.Object,
            subagents.Object,
            delegations.Object);

        var response = await service.GetTimelineAsync(_runId);

        response.RunId.Should().Be(_runId);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.ToolCall && e.Title == "read_file");
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.Permission);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.SubagentSpawn);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.SubagentComplete);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.DelegationStart);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.DelegationComplete);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.VerifyAttempt);
        response.Events.Should().Contain(e => e.Kind == SessionTimelineKind.FlowNode);
        response.Events.Should().BeInAscendingOrder(e => e.TimestampUtc);
    }
}
