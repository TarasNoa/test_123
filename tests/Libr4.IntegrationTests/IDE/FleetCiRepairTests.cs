using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CiRepairLogParserTests
{
    [Fact]
    public void TryParseRunIdFromLogsUrl_ParsesActionsRun()
    {
        CiRepairLogParser.TryParseRunIdFromLogsUrl("https://github.com/org/repo/actions/runs/9001")
            .Should().Be(9001);
    }

    [Fact]
    public void Parse_ExtractsFocusPathsAndErrors()
    {
        var raw = """
                  ## build
                  src/App.tsx(12,5): error TS2304: Cannot find name 'Foo'
                  npm ERR! Test failed
                  """;

        var parsed = CiRepairLogParser.Parse(raw);

        parsed.FocusPaths.Should().Contain("src/App.tsx");
        parsed.Errors.Should().NotBeEmpty();
        parsed.Excerpt.Should().Contain("error TS2304");
    }

    [Fact]
    public void BuildRepairTask_IncludesLogExcerptAndPaths()
    {
        var task = CiRepairLogParser.BuildRepairTask(
            ["src/App.tsx"],
            "error TS2304: Cannot find name 'Foo'",
            prefetchText: "hit: src/App.tsx");

        task.Should().Contain("src/App.tsx");
        task.Should().Contain("error TS2304");
        task.Should().Contain("Prefetched codebase context");
    }
}

public sealed class FleetCiRepairDispatchTests : IDisposable
{
    private readonly string _root;

    public FleetCiRepairDispatchTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-ci-repair-{Guid.NewGuid():N}");
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
    public async Task ApplyCiWebhook_Failure_DispatchesRepairOnce()
    {
        var runId = Guid.NewGuid();
        var store = CreateStore();
        await store.SaveAsync(new RunShipState(
            runId,
            7,
            "https://github.com/org/repo/pull/7",
            GitHubShipService.BuildHeadBranch(runId),
            FleetCiStatus.Pending,
            "https://github.com/org/repo/actions/runs/9001",
            DateTime.UtcNow));

        var fleet = new Mock<IAgentFleetRegistry>();
        fleet.Setup(x => x.UpsertFromRunAsync(runId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repair = new Mock<ICiRepairDispatcher>();
        var service = new FleetShipSyncService(
            store,
            new Lazy<IAgentFleetRegistry>(() => fleet.Object),
            NullLogger<FleetShipSyncService>.Instance,
            repair.Object,
            Options.Create(new CiRepairOptions { AutoSpawnRepairOnCiFail = true }));

        await service.ApplyCiWebhookAsync(new GitHubCiWebhookPayload(
            "workflow_run",
            "completed",
            GitHubShipService.BuildHeadBranch(runId),
            "failure",
            "https://github.com/org/repo/actions/runs/9001",
            runId));

        repair.Verify(
            x => x.DispatchCiFailureRepair(
                runId,
                "https://github.com/org/repo/actions/runs/9001"),
            Times.Once);

        await service.ApplyCiWebhookAsync(new GitHubCiWebhookPayload(
            "workflow_run",
            "completed",
            GitHubShipService.BuildHeadBranch(runId),
            "failure",
            "https://github.com/org/repo/actions/runs/9001",
            runId));

        repair.Verify(
            x => x.DispatchCiFailureRepair(
                runId,
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyCiWebhook_Failure_SkippedWhenDisabled()
    {
        var runId = Guid.NewGuid();
        var store = CreateStore();
        var fleet = new Mock<IAgentFleetRegistry>();
        fleet.Setup(x => x.UpsertFromRunAsync(runId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repair = new Mock<ICiRepairDispatcher>();
        var service = new FleetShipSyncService(
            store,
            new Lazy<IAgentFleetRegistry>(() => fleet.Object),
            NullLogger<FleetShipSyncService>.Instance,
            repair.Object,
            Options.Create(new CiRepairOptions { AutoSpawnRepairOnCiFail = false }));

        await service.ApplyCiWebhookAsync(new GitHubCiWebhookPayload(
            "workflow_run",
            "completed",
            GitHubShipService.BuildHeadBranch(runId),
            "failure",
            "https://github.com/org/repo/actions/runs/9001",
            runId));

        repair.Verify(
            x => x.DispatchCiFailureRepair(It.IsAny<Guid>(), It.IsAny<string?>()),
            Times.Never);
    }

    private FleetShipStateStore CreateStore()
    {
        var options = Options.Create(new AgentFleetOptions
        {
            IndexDbPath = Path.Combine(_root, "fleet.db"),
            RunsRoot = _root
        });
        return new FleetShipStateStore(options);
    }
}
