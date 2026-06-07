using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AgentMemorySystem;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public sealed class HermesCognitiveMemoryBridge : ICognitiveMemoryBridge
{
    private readonly SqliteHermesMemoryStore _store;
    private readonly ILogger<HermesCognitiveMemoryBridge> _logger;
    private readonly ConcurrentDictionary<string, CognitiveMemorySystem> _systems = new(StringComparer.Ordinal);

    public HermesCognitiveMemoryBridge(
        SqliteHermesMemoryStore store,
        ILogger<HermesCognitiveMemoryBridge> logger)
    {
        _store = store;
        _logger = logger;
    }

    public CognitiveMemorySystem GetOrCreateSystem(string fingerprint, string? agentId = null)
    {
        return _systems.GetOrAdd(
            fingerprint,
            _ => new CognitiveMemorySystem($"hermes:{fingerprint}", agentId ?? "autonomous-agent"));
    }

    public Task SyncFromHermesEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        var system = GetOrCreateSystem(entry.RequestFingerprint, entry.UserId);
        var fragment = ToLayeredFragment(entry);
        var existing = system.LayeredFragments.FirstOrDefault(
            fragment => fragment.Metadata.TryGetValue("hermes_id", out var hermesId)
                        && hermesId == entry.Id.ToString());
        if (existing is not null)
            system.LayeredFragments.Remove(existing);

        system.AddLayeredFragment(fragment);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<LayeredMemoryFragment>> SearchLayerAsync(
        string fingerprint,
        MemoryLayer layer,
        string query,
        int topN = 10,
        CancellationToken ct = default)
    {
        var kinds = CognitiveMemoryLayerMapper.ToKinds(layer);
        var results = await _store.RetrieveAsync(
            new HermesMemoryQuery(fingerprint, query, Math.Max(1, topN), kinds),
            ct).ConfigureAwait(false);

        return results
            .Select(result => ToLayeredFragment(result.Entry))
            .OrderByDescending(fragment => fragment.RelevanceScore)
            .Take(Math.Max(1, topN))
            .ToList();
    }

    public MemorySystemStatistics GetStatistics(string fingerprint)
    {
        if (_systems.TryGetValue(fingerprint, out var cached))
            return cached.GetStatistics();

        return new MemorySystemStatistics
        {
            SystemId = $"hermes:{fingerprint}",
            AgentId = "autonomous-agent",
            TotalFragments = 0,
            TotalLayeredFragments = 0,
            TotalSkills = 0,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<int> BackfillFromHermesAsync(CancellationToken ct = default)
    {
        var entries = await _store.ListAllAsync(ct).ConfigureAwait(false);
        foreach (var entry in entries)
            await SyncFromHermesEntryAsync(entry, ct).ConfigureAwait(false);

        _logger.LogInformation("Cognitive memory bridge backfilled {Count} Hermes entries", entries.Count);
        return entries.Count;
    }

    internal static LayeredMemoryFragment ToLayeredFragment(HermesMemoryEntry entry)
    {
        var fragment = new LayeredMemoryFragment(
            CognitiveMemoryLayerMapper.ToLayer(entry.Kind),
            $"{entry.Key}: {entry.Summary}",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["hermes_id"] = entry.Id.ToString(),
                ["run_id"] = entry.RunId.ToString(),
                ["key"] = entry.Key,
                ["stage"] = entry.Stage,
                ["fingerprint"] = entry.RequestFingerprint
            });

        fragment.UpdateRelevanceScore(HermesMemoryScoring.ComputeRelevanceScore(entry, keyword: null));
        return fragment;
    }
}
