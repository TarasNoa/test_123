using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FleetShipSyncServiceTests
{
    [Fact]
    public void TryParseRunIdFromBranch_ParsesAutogenBranch()
    {
        var runId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        FleetShipSyncService.TryParseRunIdFromBranch($"libr4/autogen-{runId:N}")
            .Should().Be(runId);
    }

    [Fact]
    public async Task RecordShipResult_PersistsPrAndUpdatesFleet()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fleet-ship-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var runId = Guid.NewGuid();
            var store = CreateStore(root);
            var fleet = new Mock<IAgentFleetRegistry>();
            fleet.Setup(x => x.UpsertFromRunAsync(runId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = new FleetShipSyncService(
                store,
                new Lazy<IAgentFleetRegistry>(() => fleet.Object),
                NullLogger<FleetShipSyncService>.Instance);

            var result = GitHubShipResult.Succeeded(
                "ok",
                workflowRunId: 1,
                pullRequestNumber: 7,
                pullRequestUrl: "https://github.com/org/repo/pull/7",
                headBranch: GitHubShipService.BuildHeadBranch(runId));

            await service.RecordShipResultAsync(runId, result);

            var state = await store.GetAsync(runId);
            state.Should().NotBeNull();
            state!.PullRequestNumber.Should().Be(7);
            state.CiStatus.Should().Be(FleetCiStatus.Pending);
            fleet.Verify(x => x.UpsertFromRunAsync(runId, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyCiWebhook_Success_MarksCiSuccess()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fleet-ci-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var runId = Guid.NewGuid();
            var store = CreateStore(root);
            await store.SaveAsync(new RunShipState(
                runId,
                7,
                "https://github.com/org/repo/pull/7",
                GitHubShipService.BuildHeadBranch(runId),
                FleetCiStatus.Pending,
                null,
                DateTime.UtcNow));

            var fleet = new Mock<IAgentFleetRegistry>();
            fleet.Setup(x => x.UpsertFromRunAsync(runId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = new FleetShipSyncService(
                store,
                new Lazy<IAgentFleetRegistry>(() => fleet.Object),
                NullLogger<FleetShipSyncService>.Instance);

            await service.ApplyCiWebhookAsync(new GitHubCiWebhookPayload(
                "workflow_run",
                "completed",
                GitHubShipService.BuildHeadBranch(runId),
                "success",
                "https://github.com/org/repo/actions/runs/1"));

            var state = await store.GetAsync(runId);
            state!.CiStatus.Should().Be(FleetCiStatus.Success);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(FleetCiStatus.Pending, AgentFleetStatus.WaitingForCi)]
    [InlineData(FleetCiStatus.Success, AgentFleetStatus.Completed)]
    [InlineData(FleetCiStatus.Failure, AgentFleetStatus.Failed)]
    public void ResolveStatus_MapsShipState(string ciStatus, AgentFleetStatus expected)
    {
        var run = AppGenerationOrchestrator.Create("demo", "fp");
        run.MarkCompleted();

        var fleetMock = new Mock<IAgentFleetRegistry>();
        var service = new FleetShipSyncService(
            new Mock<IFleetShipStateStore>().Object,
            new Lazy<IAgentFleetRegistry>(() => fleetMock.Object),
            NullLogger<FleetShipSyncService>.Instance);

        var ship = new RunShipState(
            run.Id,
            1,
            "https://github.com/org/repo/pull/1",
            GitHubShipService.BuildHeadBranch(run.Id),
            ciStatus,
            null,
            DateTime.UtcNow);

        service.ResolveStatus(run, ship).Should().Be(expected);
    }

    private static FleetShipStateStore CreateStore(string root)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AgentFleetOptions { RunsRoot = root });
        return new FleetShipStateStore(options);
    }
}
