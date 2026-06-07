using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentFleetRegistryTests : IDisposable
{
    private readonly string _root;

    public AgentFleetRegistryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"agent-fleet-{Guid.NewGuid():N}");
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
    public async Task UpsertAndList_IndexesRunWithPlanningStatus()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();

        await registry.UpsertFromRunAsync(runId);

        var items = await registry.ListAsync(new AgentFleetListQuery());
        items.Should().ContainSingle(i => i.RunId == runId);
        items.Single(i => i.RunId == runId).Status.Should().Be(AgentFleetStatus.Planning);
    }

    [Fact]
    public async Task Patch_UpdatesTitleAndPinned()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();
        await registry.UpsertFromRunAsync(runId);

        await registry.PatchAsync(runId, new AgentFleetPatchRequest(Title: "Calorie Vision", Pinned: true));

        var detail = await registry.GetSummaryAsync(runId);
        detail.Should().NotBeNull();
        detail!.Entry.Title.Should().Be("Calorie Vision");
        detail.Entry.Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task ActiveRunControl_MapsRepairingStatus()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var runControl = new AutonomousRunControlService();
        runControl.RegisterRun(runId, new CancellationTokenSource());
        runControl.UpdateRunProgress(runId, "repair-loop", 2, 1);

        var registry = CreateRegistry(repo, runControl);
        await registry.EnsureSchemaAsync();
        await registry.UpsertFromRunAsync(runId);

        var detail = await registry.GetSummaryAsync(runId);
        detail!.Entry.Status.Should().Be(AgentFleetStatus.Repairing);
        detail.Entry.Stage.Should().Be("repair-loop");
    }

    [Fact]
    public async Task PromoteStateJson_MapsHandoffPendingStatus()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();

        var handoffDir = Path.Combine(_root, runId.ToString("D"), "handoff");
        Directory.CreateDirectory(handoffDir);
        await File.WriteAllTextAsync(
            Path.Combine(handoffDir, "promote-state.json"),
            """{"status":"HandoffPending","exportId":"exp-1"}""");

        await registry.UpsertFromRunAsync(runId);

        var detail = await registry.GetSummaryAsync(runId);
        detail!.Entry.Status.Should().Be(AgentFleetStatus.HandoffPending);
        detail.Entry.Stage.Should().Be("handoff_pending");
    }

    [Fact]
    public async Task ShipState_MapsWaitingForCiStatus()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();

        var shipDir = Path.Combine(_root, runId.ToString("D"), "ship");
        Directory.CreateDirectory(shipDir);
        var state = new RunShipState(
            runId,
            12,
            "https://github.com/org/repo/pull/12",
            GitHubShipService.BuildHeadBranch(runId),
            FleetCiStatus.Pending,
            null,
            DateTime.UtcNow);
        await File.WriteAllTextAsync(
            Path.Combine(shipDir, "state.json"),
            System.Text.Json.JsonSerializer.Serialize(state));

        await registry.UpsertFromRunAsync(runId);

        var detail = await registry.GetSummaryAsync(runId);
        detail!.Entry.Status.Should().Be(AgentFleetStatus.WaitingForCi);
        detail.Entry.PrUrl.Should().Contain("pull/12");
        detail.Entry.CiStatus.Should().Be(FleetCiStatus.Pending);
    }

    [Fact]
    public async Task PlaybookStats_SurfaceOnFleetSummary()
    {
        var repo = new StubRepository(Guid.Empty);
        var runId = repo.RunId;
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();

        var runDir = Path.Combine(_root, runId.ToString("D"));
        Directory.CreateDirectory(runDir);
        RunPlaybookStats.RecordAttempt(runDir);
        RunPlaybookStats.RecordAttempt(runDir);
        RunPlaybookStats.RecordHit(runDir);

        await registry.UpsertFromRunAsync(runId);

        var items = await registry.ListAsync(new AgentFleetListQuery());
        var item = items.Single(i => i.RunId == runId);
        item.PlaybookAttempts.Should().Be(2);
        item.PlaybookHits.Should().Be(1);
    }

    private AgentFleetRegistry CreateRegistry(
        IAppGenerationRepository repository,
        IAutonomousRunControlService? runControl = null)
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions
        {
            IndexDbPath = dbPath,
            RunsRoot = _root
        });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var shipState = new FleetShipStateStore(options);
        var flow = new Mock<IFlowProgressStore>();
        flow.Setup(x => x.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowProgress?)null);

        AgentFleetRegistry registry = null!;
        var shipSync = new FleetShipSyncService(
            shipState,
            new Lazy<IAgentFleetRegistry>(() => registry),
            NullLogger<FleetShipSyncService>.Instance);

        registry = new AgentFleetRegistry(
            index,
            repository,
            runControl ?? new AutonomousRunControlService(),
            flow.Object,
            options,
            NullLogger<AgentFleetRegistry>.Instance,
            shipState: shipState,
            shipSync: shipSync);

        return registry;
    }

    private sealed class StubRepository : IAppGenerationRepository
    {
        private readonly AppGenerationOrchestrator _run;

        public StubRepository(Guid runId)
        {
            _run = AppGenerationOrchestrator.Create("build calorie tracker", "fp-test");
        }

        public Guid RunId => _run.Id;

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(id == _run.Id ? _run : null);

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_run);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_run]);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            ListAsync(ct);
    }
}
