using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Application.CodeSearch;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class QdrantSyncTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _rolloutDbPath;
    private readonly string _runsRoot;
    private readonly SqliteHermesMemoryStore _innerStore;
    private readonly FileRolloutRecorder _rolloutRecorder;
    private readonly InProcessVectorMemoryStore _vectorStore;
    private readonly DeterministicEmbeddingService _embeddings;
    private readonly HermesVectorSyncService _syncService;

    public QdrantSyncTests()
    {
        var suffix = Guid.NewGuid().ToString("N");
        _dbPath = Path.Combine(Path.GetTempPath(), $"qdrant-sync-{suffix}.db");
        _rolloutDbPath = Path.Combine(Path.GetTempPath(), $"qdrant-sync-rollout-{suffix}.db");
        _runsRoot = Path.Combine(Path.GetTempPath(), $"qdrant-sync-runs-{suffix}");
        Directory.CreateDirectory(_runsRoot);
        _innerStore = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        _rolloutRecorder = new FileRolloutRecorder(Options.Create(new AgentRuntimeOptions
        {
            RolloutDbPath = _rolloutDbPath,
            RunsRoot = _runsRoot
        }));
        _vectorStore = new InProcessVectorMemoryStore();
        _embeddings = new DeterministicEmbeddingService(dimensions: 384);
        _syncService = new HermesVectorSyncService(
            _innerStore,
            _vectorStore,
            _embeddings,
            Options.Create(new QdrantSyncOptions
            {
                UseQdrantSync = true,
                CollectionId = "test-hermes-l2"
            }),
            NullLogger<HermesVectorSyncService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
            if (File.Exists(_rolloutDbPath))
                File.Delete(_rolloutDbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void ReciprocalRankFusion_PrefersItemsPresentInBothLists()
    {
        var fused = ReciprocalRankFusion.Fuse([
            ["a|one", "b|two"],
            ["b|two", "c|three"]
        ]);

        fused.Should().NotBeEmpty();
        fused[0].Id.Should().Be("b|two");
        fused[0].Score.Should().BeGreaterThan(fused[^1].Score);
    }

    [Fact]
    public async Task DeterministicEmbeddingService_Embed_IsStable()
    {
        var first = await _embeddings.EmbedAsync("django manage.py invalid json");
        var second = await _embeddings.EmbedAsync("django manage.py invalid json");

        first.Should().Equal(second);
        first.Length.Should().Be(384);
    }

    [Fact]
    public async Task QdrantSyncDecorator_SyncsVectorIndexOnUpsert()
    {
        var decorated = new QdrantSyncHermesMemoryStore(_innerStore, _syncService);
        var runId = Guid.NewGuid();
        const string fingerprint = "fp-qdrant";
        const string key = "django-settings";

        await decorated.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            fingerprint,
            MemoryKind.Semantic,
            "build",
            key,
            "invalid json in manage.py settings block",
            null,
            32,
            0.9,
            DateTime.UtcNow));

        var queryEmbedding = await _embeddings.EmbedAsync("manage.py invalid json settings", CancellationToken.None);
        var vectorHits = await _vectorStore.SearchAsync(
            queryEmbedding,
            "test-hermes-l2",
            topK: 5,
            minScore: 0.0);

        vectorHits.Should().NotBeEmpty();
        vectorHits[0].Record.Metadata.Should().ContainKey("key");
        vectorHits[0].Record.Metadata!["key"].Should().Be(key);
    }

    [Fact]
    public async Task HybridSearch_FusesFtsAndVectorMemoryHits()
    {
        var decorated = new QdrantSyncHermesMemoryStore(_innerStore, _syncService);
        var runId = Guid.NewGuid();
        const string fingerprint = "fp-hybrid";
        const string key = "settings-json";

        await decorated.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            fingerprint,
            MemoryKind.Semantic,
            "repair",
            key,
            "manage.py invalid json after settings merge",
            null,
            40,
            0.8,
            DateTime.UtcNow));

        var search = new HybridSessionSearchService(
            _rolloutRecorder,
            decorated,
            _vectorStore,
            _embeddings,
            Options.Create(new QdrantSyncOptions
            {
                UseQdrantSync = true,
                CollectionId = "test-hermes-l2",
                HybridSearchCandidateMultiplier = 2
            }));

        var hits = await search.SearchAsync("manage.py invalid json", limit: 5);
        hits.Should().NotBeEmpty();
        hits.Should().Contain(hit => hit.Source == "memory" && hit.MemoryKey == key);
    }

    private sealed class DeterministicEmbeddingService : IEmbeddingService
    {
        private readonly int _dimensions;

        public DeterministicEmbeddingService(int dimensions) => _dimensions = dimensions;

        public int Dimensions => _dimensions;

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var vec = new float[_dimensions];
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(text));

            for (var i = 0; i < _dimensions; i++)
                vec[i] = (bytes[i % 32] / 255f) * 2f - 1f;

            var norm = MathF.Sqrt(vec.Sum(v => v * v));
            if (norm > 0)
            {
                for (var i = 0; i < vec.Length; i++)
                    vec[i] /= norm;
            }

            return Task.FromResult(vec);
        }

        public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
        {
            var results = new float[texts.Count][];
            for (var i = 0; i < texts.Count; i++)
                results[i] = await EmbedAsync(texts[i], ct).ConfigureAwait(false);
            return results;
        }
    }
}
