using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FleetRunQualityCalculatorTests : IDisposable
{
    private readonly string _root;

    public FleetRunQualityCalculatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-quality-{Guid.NewGuid():N}");
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
    public void Compute_HighQualityRun_ScoresAboveEighty()
    {
        var runId = Guid.NewGuid();
        var runDir = Path.Combine(_root, runId.ToString("D"));
        Directory.CreateDirectory(runDir);

        var run = AppGenerationOrchestrator.Create("demo", "fp");
        run.RecordQualityGate("verify_subagent", 9, passed: true, ["obscura passed"]);

        File.WriteAllText(
            Path.Combine(runDir, "rollout.jsonl"),
            """
            {"type":"tool_use","toolName":"apply_patch","timestamp":1700000000000}
            {"type":"tool_use","toolName":"read_file","timestamp":1700000001000}
            """);

        RunPlaybookStats.RecordAttempt(runDir);
        RunPlaybookStats.RecordHit(runDir);

        var quality = FleetRunQualityCalculator.Compute(run, runDir, RunPlaybookStats.Read(runDir));

        quality.Score.Should().BeGreaterThanOrEqualTo(80);
        quality.VerifyPoints.Should().Be(35);
        quality.PatchCount.Should().Be(1);
    }

    [Fact]
    public async Task Registry_SurfacesQualityScoreOnFleetSummary()
    {
        var orchestrator = AppGenerationOrchestrator.Create("quality run", "fp-q");
        var runId = orchestrator.Id;
        var runDir = Path.Combine(_root, runId.ToString("D"));
        Directory.CreateDirectory(runDir);

        orchestrator.RecordQualityGate("verify_subagent", 9, passed: true, ["ok"]);
        orchestrator.MarkCompleted();

        var repo = new QualityStubRepository(orchestrator);
        var registry = CreateRegistry(repo, _root);
        await registry.EnsureSchemaAsync();
        await registry.UpsertFromRunAsync(runId);

        var summary = await registry.GetSummaryAsync(runId);
        summary!.Entry.QualityScore.Should().BeGreaterThan(0);

        var list = await registry.ListAsync(new AgentFleetListQuery(SortBy: "quality"));
        list.Should().ContainSingle(i => i.RunId == runId && i.QualityScore > 0);
    }

    private AgentFleetRegistry CreateRegistry(IAppGenerationRepository repository, string runsRoot)
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions
        {
            IndexDbPath = dbPath,
            RunsRoot = runsRoot
        });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var flow = new Mock<Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow.IFlowProgressStore>();
        flow.Setup(x => x.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow.FlowProgress?)null);

        return new AgentFleetRegistry(
            index,
            repository,
            new AutonomousRunControlService(),
            flow.Object,
            options,
            NullLogger<AgentFleetRegistry>.Instance);
    }

    private sealed class QualityStubRepository : IAppGenerationRepository
    {
        private AppGenerationOrchestrator _run;

        public QualityStubRepository(AppGenerationOrchestrator run) => _run = run;

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(id == _run.Id ? _run : null);

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_run);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
        {
            _run = orchestrator;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_run]);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            ListAsync(ct);
    }
}
