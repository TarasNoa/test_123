using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class HermesMemoryManagerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _store;
    private readonly HermesMemoryManager _manager;

    public HermesMemoryManagerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"hermes-mgr-{Guid.NewGuid():N}.db");
        _store = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        _manager = new HermesMemoryManager(
            _store,
            Options.Create(new HermesMemoryManagerOptions()),
            NullLogger<HermesMemoryManager>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task PrefetchBeforeTurn_FormatsRelevantMemorySection()
    {
        var runId = Guid.NewGuid();
        var fp = "django-app|django,python";
        var plan = SamplePlan();
        var resolved = _manager.ResolveFingerprint(plan, fp);

        await _store.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(), runId, null, resolved, MemoryKind.Semantic, "repair", "import-fix",
            "Use relative imports in Django apps", null, 20, 1.0, DateTime.UtcNow));

        var nudge = await _manager.PrefetchBeforeTurnAsync(
            new HermesTurnContext(runId, resolved, "repair", ["django"]));

        nudge.Should().Contain("## relevant_memory");
        nudge.Should().Contain("[L2_semantic]");
        nudge.Should().Contain("import-fix");
        nudge.Should().Contain("score:");
    }

    [Fact]
    public async Task SyncAfterTool_IngestsSuccessfulPatchAsProcedural()
    {
        var runId = Guid.NewGuid();
        var fp = "fp-tool-ingest";
        var ctx = new HermesTurnContext(runId, fp, "repair");

        await _manager.SyncAfterToolAsync(
            ctx,
            "apply_patch",
            new string('x', 80),
            success: true);

        var results = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 5));
        results.Should().HaveCount(1);
        results[0].Entry.Kind.Should().Be(MemoryKind.Procedural);
    }

    [Fact]
    public async Task OnPreCompact_ConsolidatesEpisodicIntoSemantic()
    {
        var runId = Guid.NewGuid();
        var fp = "fp-compact";
        var ctx = new HermesTurnContext(runId, fp, "repair");

        await _store.UpsertAsync(MakeEpisodic(runId, fp, "e1", "error one"));
        await _store.UpsertAsync(MakeEpisodic(runId, fp, "e2", "error two"));

        await _manager.OnPreCompactAsync(ctx);

        var semantic = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 10, Kinds: [MemoryKind.Semantic]));
        semantic.Should().ContainSingle();
        semantic[0].Entry.Key.Should().StartWith("consolidated:");
    }

    private static GenerationPlan SamplePlan() =>
        new(
            "DjangoApp",
            "Calorie tracker",
            new TechStack(["Python"], ["Django"], [], [], "django"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12-slim",
            Array.Empty<string>(),
            Array.Empty<string>());

    private static HermesMemoryEntry MakeEpisodic(Guid runId, string fp, string key, string summary) =>
        new(Guid.NewGuid(), runId, null, fp, MemoryKind.Episodic, "repair", key, summary, null, 10, 0, DateTime.UtcNow);
}
