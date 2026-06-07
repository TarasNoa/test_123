using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DreamConsolidationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _store;
    private readonly HermesDreamConsolidationService _service;

    public DreamConsolidationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dream-consolidation-{Guid.NewGuid():N}.db");
        _store = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath, EpisodicRetentionDays = 90 }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        _service = new HermesDreamConsolidationService(
            _store,
            Options.Create(new DreamConsolidationOptions
            {
                MinHashSimilarityThreshold = 0.65,
                MinEpisodicClusterSize = 2,
                MinScoreThreshold = 0.1,
                StaleAgeDays = 30
            }),
            NullLogger<HermesDreamConsolidationService>.Instance);
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
    public void MinHashSimilarity_IdenticalTexts_AreHighlySimilar()
    {
        var left = MinHashSimilarity.ComputeSignature("manage.py invalid json settings");
        var right = MinHashSimilarity.ComputeSignature("manage.py invalid json settings");
        MinHashSimilarity.EstimateSimilarity(left, right).Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task RunAsync_MergesSimilarEpisodicIntoSemantic()
    {
        var runId = Guid.NewGuid();
        const string fp = "fp-dream";
        await _store.UpsertAsync(MakeEntry(runId, fp, "ep-1", MemoryKind.Episodic, "manage.py invalid json in settings block"));
        await _store.UpsertAsync(MakeEntry(runId, fp, "ep-2", MemoryKind.Episodic, "manage.py invalid json in settings block duplicate"));
        await _store.UpsertAsync(MakeEntry(runId, fp, "ep-3", MemoryKind.Episodic, "unrelated postgres connection timeout"));

        var result = await _service.RunAsync();

        result.Success.Should().BeTrue();
        result.EpisodicMergedToSemantic.Should().BeGreaterOrEqualTo(1);

        var all = await _store.ListAllAsync();
        all.Should().Contain(entry => entry.Kind == MemoryKind.Semantic && entry.Stage == "dream_consolidation");
        all.Count(entry => entry.Kind == MemoryKind.Episodic).Should().BeLessThan(3);
    }

    [Fact]
    public async Task RunAsync_PrunesStaleLowScoreMemories()
    {
        var runId = Guid.NewGuid();
        const string fp = "fp-stale";
        await _store.UpsertAsync(MakeEntry(
            runId,
            fp,
            "stale",
            MemoryKind.Procedural,
            "old low value note",
            score: 0.05,
            createdAt: DateTime.UtcNow.AddDays(-60)));

        var result = await _service.RunAsync();
        result.Success.Should().BeTrue();
        result.StalePruned.Should().BeGreaterOrEqualTo(1);

        var all = await _store.ListAllAsync();
        all.Should().NotContain(entry => entry.Key == "stale");
    }

    [Fact]
    public async Task RunAsync_DeduplicatesNearDuplicateSemanticRows()
    {
        var runId = Guid.NewGuid();
        const string fp = "fp-dedupe";
        await _store.UpsertAsync(MakeEntry(runId, fp, "sem-a", MemoryKind.Semantic, "django import settings module error in manage.py", score: 2.0));
        await _store.UpsertAsync(MakeEntry(runId, fp, "sem-b", MemoryKind.Semantic, "django import settings module error in settings.py", score: 1.0));

        var result = await _service.RunAsync();
        result.Success.Should().BeTrue();
        result.DuplicatesRemoved.Should().BeGreaterOrEqualTo(1);

        var all = await _store.ListAllAsync();
        all.Count(entry => entry.Kind == MemoryKind.Semantic && entry.Key.StartsWith("sem-", StringComparison.Ordinal)).Should().Be(1);
    }

    [Fact]
    public void NightlyHostedService_RunsOncePerUtcDayAtConfiguredHour()
    {
        var service = new DreamConsolidationNightlyHostedService(
            _service,
            Options.Create(new DreamConsolidationOptions { NightlyHourUtc = 3 }),
            NullLogger<DreamConsolidationNightlyHostedService>.Instance);

        service.ShouldRunNow().Should().Be(DateTime.UtcNow.Hour == 3);
    }

    private static HermesMemoryEntry MakeEntry(
        Guid runId,
        string fingerprint,
        string key,
        MemoryKind kind,
        string summary,
        double score = 1.0,
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
            Tokens: Math.Max(1, summary.Length / 4),
            score,
            createdAt ?? DateTime.UtcNow);
}
