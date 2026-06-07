using System.Diagnostics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FleetSessionSearchTests : IDisposable
{
    private readonly string _root;

    public FleetSessionSearchTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-search-{Guid.NewGuid():N}");
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
    public async Task Search_FindsUniqueErrorSignature()
    {
        var (search, index, runId) = CreateServices();
        await index.EnsureSchemaAsync();
        await search.EnsureSchemaAsync();
        await index.UpsertAsync(new AgentFleetEntry(
            runId,
            "CalorieVision",
            null,
            AgentFleetStatus.Failed,
            "repair-loop",
            1,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            0,
            null,
            null,
            "django+solidjs",
            false,
            false,
            "UNIQUE_MANAGE_PY_JSON_PARSE_ERROR_7f3a"));

        await search.IndexAsync(new FleetSessionIndexDocument(
            runId,
            "CalorieVision",
            "build calorie tracker django",
            "UNIQUE_MANAGE_PY_JSON_PARSE_ERROR_7f3a",
            "backend/manage.py",
            null,
            "django+solidjs",
            "fail",
            DateTime.UtcNow,
            false));

        var result = await search.SearchAsync(new FleetSessionSearchQuery("UNIQUE_MANAGE_PY_JSON_PARSE_ERROR_7f3a"));
        result.Hits.Should().ContainSingle(h => h.RunId == runId);
    }

    [Fact]
    public async Task ForkRun_PreservesPlanAndCreatesNewRunId()
    {
        var repo = new ForkStubRepository();
        var registry = CreateRegistry(repo);
        await registry.EnsureSchemaAsync();
        await registry.UpsertFromRunAsync(repo.RunId);

        var fork = new RunForkService(repo, registry, Options.Create(new AgentFleetOptions { RunsRoot = _root }), NullLogger<RunForkService>.Instance);
        var result = await fork.ForkAsync(repo.RunId);

        result.Should().NotBeNull();
        result!.SourceRunId.Should().Be(repo.RunId);
        result.NewRunId.Should().NotBe(repo.RunId);
        result.PlanCopied.Should().BeTrue();

        var forked = await repo.GetAsync(result.NewRunId);
        forked!.Plan.Should().NotBeNull();
        forked.Plan!.ApplicationName.Should().Be("DemoApp");
        forked.Files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Search_500Documents_CompletesUnderTwoHundredMs()
    {
        var (search, index, _) = CreateServices();
        await index.EnsureSchemaAsync();
        await search.EnsureSchemaAsync();

        for (var i = 0; i < 500; i++)
        {
            var runId = Guid.NewGuid();
            await index.UpsertAsync(new AgentFleetEntry(
                runId,
                $"Run {i}",
                null,
                AgentFleetStatus.Completed,
                "done",
                1,
                DateTime.UtcNow,
                DateTime.UtcNow,
                0,
                null,
                null,
                "django",
                false,
                false,
                null));

            await search.IndexAsync(new FleetSessionIndexDocument(
                runId,
                $"Run {i}",
                $"request token bench{i}",
                i == 499 ? "BENCH_TARGET_TOKEN_XYZ" : null,
                $"src/file{i}.py",
                null,
                "django",
                "pass",
                DateTime.UtcNow,
                false));
        }

        var sw = Stopwatch.StartNew();
        var result = await search.SearchAsync(new FleetSessionSearchQuery("BENCH_TARGET_TOKEN_XYZ", Limit: 10));
        sw.Stop();

        result.Hits.Should().ContainSingle();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    private (SqliteFleetSessionSearchService Search, SqliteAgentFleetIndexStore Index, Guid RunId) CreateServices()
    {
        var runId = Guid.NewGuid();
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions { IndexDbPath = dbPath, RunsRoot = _root });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var search = new SqliteFleetSessionSearchService(options, index, NullLogger<SqliteFleetSessionSearchService>.Instance);
        return (search, index, runId);
    }

    private AgentFleetRegistry CreateRegistry(IAppGenerationRepository repository)
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions { IndexDbPath = dbPath, RunsRoot = _root });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var search = new SqliteFleetSessionSearchService(options, index, NullLogger<SqliteFleetSessionSearchService>.Instance);
        var flow = new Mock<IFlowProgressStore>();
        flow.Setup(x => x.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowProgress?)null);

        AgentFleetRegistry registry = null!;
        var shipSync = new FleetShipSyncService(
            new FleetShipStateStore(options),
            new Lazy<IAgentFleetRegistry>(() => registry),
            NullLogger<FleetShipSyncService>.Instance);

        registry = new AgentFleetRegistry(
            index,
            repository,
            new AutonomousRunControlService(),
            flow.Object,
            options,
            NullLogger<AgentFleetRegistry>.Instance,
            shipState: new FleetShipStateStore(options),
            shipSync: shipSync,
            sessionSearch: search);

        return registry;
    }

    private sealed class ForkStubRepository : IAppGenerationRepository
    {
        private readonly AppGenerationOrchestrator _run;

        public ForkStubRepository()
        {
            _run = AppGenerationOrchestrator.Create("build demo app", "fp-fork");
            _run.AttachPlan(new GenerationPlan(
                applicationName: "DemoApp",
                applicationDescription: "Demo",
                techStack: new TechStack(["Python"], ["Django"], [], [], "django"),
                phases: Array.Empty<GenerationPhase>(),
                requiredAgents: Array.Empty<string>(),
                runtimeImage: "python:3.12",
                buildCommands: ["python manage.py check"],
                testCommands: ["python manage.py test"]));
            _run.UpsertFile(new GeneratedFile("backend/manage.py", "python", "print('ok')"));
        }

        public Guid RunId => _run.Id;

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default)
        {
            if (id == _run.Id)
                return Task.FromResult<AppGenerationOrchestrator?>(_run);
            return Task.FromResult(_saved.FirstOrDefault(r => r.Id == id));
        }

        private readonly List<AppGenerationOrchestrator> _saved = new();

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_run);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
        {
            _saved.RemoveAll(r => r.Id == orchestrator.Id);
            _saved.Add(orchestrator);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_run, .. _saved]);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            ListAsync(ct);
    }
}
