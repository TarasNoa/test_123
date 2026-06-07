using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class HermesMemoryStoreTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _store;

    public HermesMemoryStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"hermes-test-{Guid.NewGuid():N}.db");
        _store = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath, EpisodicRetentionDays = 90 }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
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
    public async Task EnsureSchema_CreatesMemoriesTable()
    {
        await _store.EnsureSchemaAsync();
        File.Exists(_dbPath).Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_ReplacesSameFingerprintAndKey()
    {
        var fp = "req-fp-1";
        var runId = Guid.NewGuid();
        await _store.UpsertAsync(MakeEntry(runId, fp, "k1", MemoryKind.Procedural, "first"));
        await _store.UpsertAsync(MakeEntry(runId, fp, "k1", MemoryKind.Procedural, "second"));

        var results = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 10));
        results.Should().HaveCount(1);
        results[0].Entry.Summary.Should().Be("second");
    }

    [Fact]
    public async Task Retrieve_OrdersByRelevanceAndSupportsKinds()
    {
        var fp = "req-fp-2";
        var runId = Guid.NewGuid();
        await _store.UpsertAsync(MakeEntry(runId, fp, "ep", MemoryKind.Episodic, "episodic note"));
        await _store.UpsertAsync(MakeEntry(runId, fp, "proc", MemoryKind.Procedural, "fix django imports"));
        await _store.UpsertAsync(MakeEntry(runId, fp, "sem", MemoryKind.Semantic, "django stack facts"));

        var proceduralOnly = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 5, Kinds: [MemoryKind.Procedural]));
        proceduralOnly.Should().HaveCount(1);
        proceduralOnly[0].RetrievalReason.Should().Contain("L1_procedural");

        var keyword = await _store.RetrieveAsync(new HermesMemoryQuery(fp, Keyword: "django", TopK: 5));
        keyword.Should().HaveCountGreaterOrEqualTo(2);
        keyword[0].RelevanceScore.Should().BeGreaterThan(keyword[^1].RelevanceScore);
    }

    [Fact]
    public async Task PruneExpiredEpisodic_DeletesOnlyL0()
    {
        var fp = "req-fp-3";
        var runId = Guid.NewGuid();
        var old = MakeEntry(runId, fp, "old-ep", MemoryKind.Episodic, "old", createdAt: DateTime.UtcNow.AddDays(-120));
        var fresh = MakeEntry(runId, fp, "fresh-ep", MemoryKind.Episodic, "fresh");
        var semantic = MakeEntry(runId, fp, "sem", MemoryKind.Semantic, "permanent fact", createdAt: DateTime.UtcNow.AddDays(-200));

        await _store.UpsertAsync(old);
        await _store.UpsertAsync(fresh);
        await _store.UpsertAsync(semantic);

        var pruned = await _store.PruneExpiredEpisodicAsync();
        pruned.Should().Be(1);

        var remaining = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 10));
        remaining.Select(r => r.Entry.Key).Should().BeEquivalentTo(["fresh-ep", "sem"]);
    }

    [Fact]
    public async Task IMemoryStoreAdapter_RoundTripsMemoryRecord()
    {
        IMemoryStore memory = _store;
        var fp = "req-fp-4";
        var runId = Guid.NewGuid();
        await memory.IngestAsync(new MemoryRecord(
            runId, fp, "repair", MemoryKind.Procedural, "import-fix",
            "Use relative imports in Django apps", null, 42, DateTime.UtcNow), CancellationToken.None);

        var retrieved = await memory.RetrieveAsync(new MemoryQuery(fp, Keyword: "django", TopK: 3), CancellationToken.None);
        retrieved.Should().HaveCount(1);
        retrieved[0].Record.Key.Should().Be("import-fix");
        retrieved[0].RetrievalReason.Should().Contain("L1_procedural");
    }

    [Fact]
    public async Task PruneByTokenBudget_KeepsHighestScoredWithinBudget()
    {
        var fp = "req-fp-5";
        var runId = Guid.NewGuid();
        await _store.UpsertAsync(MakeEntry(runId, fp, "low", MemoryKind.Episodic, "low", tokens: 100, score: 0.1));
        await _store.UpsertAsync(MakeEntry(runId, fp, "high", MemoryKind.Procedural, "high", tokens: 100, score: 5.0));
        await _store.UpsertAsync(MakeEntry(runId, fp, "mid", MemoryKind.Semantic, "mid", tokens: 100, score: 2.0));

        await _store.PruneByTokenBudgetAsync(fp, maxTokenBudget: 150);

        var remaining = await _store.RetrieveAsync(new HermesMemoryQuery(fp, TopK: 10));
        remaining.Select(r => r.Entry.Key).Should().BeEquivalentTo(["high"]);
    }

    private static HermesMemoryEntry MakeEntry(
        Guid runId,
        string fingerprint,
        string key,
        MemoryKind kind,
        string summary,
        int tokens = 10,
        double score = 0,
        DateTime? createdAt = null) =>
        new(
            Guid.NewGuid(),
            runId,
            UserId: null,
            fingerprint,
            kind,
            Stage: "test",
            key,
            summary,
            PayloadJson: null,
            tokens,
            score,
            createdAt ?? DateTime.UtcNow);
}
