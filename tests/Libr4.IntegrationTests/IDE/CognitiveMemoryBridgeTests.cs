using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CognitiveMemoryBridgeTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _innerStore;
    private readonly HermesCognitiveMemoryBridge _bridge;

    public CognitiveMemoryBridgeTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cognitive-bridge-{Guid.NewGuid():N}.db");
        _innerStore = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        _bridge = new HermesCognitiveMemoryBridge(
            _innerStore,
            NullLogger<HermesCognitiveMemoryBridge>.Instance);
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
    public void LayerMapper_MapsHermesKindsToCognitiveLayers()
    {
        CognitiveMemoryLayerMapper.ToLayer(MemoryKind.Episodic).Should().Be(MemoryLayer.SessionArchive);
        CognitiveMemoryLayerMapper.ToLayer(MemoryKind.Procedural).Should().Be(MemoryLayer.TaskSkills);
        CognitiveMemoryLayerMapper.ToLayer(MemoryKind.Semantic).Should().Be(MemoryLayer.GlobalFacts);
        CognitiveMemoryLayerMapper.ToLayer(MemoryKind.Strategic).Should().Be(MemoryLayer.InsightIndex);
        CognitiveMemoryLayerMapper.ToLayer(MemoryKind.Meta).Should().Be(MemoryLayer.MetaRules);
    }

    [Fact]
    public async Task CognitiveSyncDecorator_ProjectsUpsertsIntoLayeredFragments()
    {
        var decorated = new CognitiveSyncHermesMemoryStore(_innerStore, _bridge);
        const string fingerprint = "fp-cognitive";
        var runId = Guid.NewGuid();

        await decorated.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            fingerprint,
            MemoryKind.Procedural,
            "repair",
            "django-settings",
            "invalid json in manage.py settings block",
            null,
            32,
            0.9,
            DateTime.UtcNow));

        var system = _bridge.GetOrCreateSystem(fingerprint);
        system.LayeredFragments.Should().HaveCount(1);
        system.LayeredFragments[0].Layer.Should().Be(MemoryLayer.TaskSkills);
        system.LayeredFragments[0].Content.Should().Contain("django-settings");
    }

    [Fact]
    public async Task SearchLayerAsync_ReadsFromHermesSourceOfTruth()
    {
        var decorated = new CognitiveSyncHermesMemoryStore(_innerStore, _bridge);
        const string fingerprint = "fp-search-layer";
        var runId = Guid.NewGuid();

        await decorated.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            fingerprint,
            MemoryKind.Semantic,
            "build",
            "settings-json",
            "manage.py invalid json after settings merge",
            null,
            40,
            0.8,
            DateTime.UtcNow));

        var hits = await _bridge.SearchLayerAsync(
            fingerprint,
            MemoryLayer.GlobalFacts,
            "manage.py invalid json",
            topN: 5);

        hits.Should().NotBeEmpty();
        hits[0].Layer.Should().Be(MemoryLayer.GlobalFacts);
        hits[0].Content.Should().Contain("settings-json");
    }

    [Fact]
    public async Task HermesBackedMemoryStore_RoutesIngestThroughDecoratorChain()
    {
        var decorated = new CognitiveSyncHermesMemoryStore(_innerStore, _bridge);
        var memory = new HermesBackedMemoryStore(decorated);
        const string fingerprint = "fp-backed-store";
        var runId = Guid.NewGuid();

        await memory.IngestAsync(
            new Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.MemoryRecord(
                runId,
                fingerprint,
                "repair",
                MemoryKind.Procedural,
                "route-test",
                "patched manage.py invalid json",
                null,
                24,
                DateTime.UtcNow),
            CancellationToken.None);

        var system = _bridge.GetOrCreateSystem(fingerprint);
        system.LayeredFragments.Should().NotBeEmpty();
        system.LayeredFragments[0].Metadata["key"].Should().Be("route-test");
    }
}
