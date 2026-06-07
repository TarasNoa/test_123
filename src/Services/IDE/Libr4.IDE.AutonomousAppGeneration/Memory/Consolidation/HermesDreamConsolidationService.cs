using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public sealed class HermesDreamConsolidationService : IDreamConsolidationService
{
    private readonly IHermesMemoryStore _store;
    private readonly DreamConsolidationOptions _options;
    private readonly ILogger<HermesDreamConsolidationService> _logger;
    private DreamConsolidationResult? _lastResult;

    public HermesDreamConsolidationService(
        IHermesMemoryStore store,
        IOptions<DreamConsolidationOptions> options,
        ILogger<HermesDreamConsolidationService> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public DreamConsolidationResult? GetLastResult() => _lastResult;

    public async Task<DreamConsolidationResult> RunAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            var skipped = new DreamConsolidationResult(
                DateTime.UtcNow,
                DateTime.UtcNow,
                TotalBefore: 0,
                EpisodicMergedToSemantic: 0,
                StalePruned: 0,
                DuplicatesRemoved: 0,
                EpisodicRetentionPruned: 0,
                SemanticAfter: 0,
                Success: true);
            _lastResult = skipped;
            return skipped;
        }

        var started = DateTime.UtcNow;
        try
        {
            await _store.EnsureSchemaAsync(ct).ConfigureAwait(false);
            var entries = await _store.ListAllAsync(ct).ConfigureAwait(false);
            var totalBefore = entries.Count;

            var episodicRetentionPruned = await _store.PruneExpiredEpisodicAsync(ct).ConfigureAwait(false);
            entries = await _store.ListAllAsync(ct).ConfigureAwait(false);

            var staleCutoff = DateTime.UtcNow.AddDays(-_options.StaleAgeDays);
            var staleIds = entries
                .Where(entry => entry.Score < _options.MinScoreThreshold && entry.CreatedAtUtc < staleCutoff)
                .Select(entry => entry.Id)
                .ToList();
            var stalePruned = await _store.DeleteByIdsAsync(staleIds, ct).ConfigureAwait(false);
            entries = await _store.ListAllAsync(ct).ConfigureAwait(false);

            var duplicateIds = FindDuplicateIds(entries);
            var duplicatesRemoved = await _store.DeleteByIdsAsync(duplicateIds, ct).ConfigureAwait(false);
            entries = await _store.ListAllAsync(ct).ConfigureAwait(false);

            var (mergedCount, mergedDeletes) = await MergeEpisodicToSemanticAsync(entries, ct).ConfigureAwait(false);
            await _store.DeleteByIdsAsync(mergedDeletes, ct).ConfigureAwait(false);

            var finalEntries = await _store.ListAllAsync(ct).ConfigureAwait(false);
            var semanticAfter = finalEntries.Count(entry => entry.Kind == MemoryKind.Semantic);

            var result = new DreamConsolidationResult(
                started,
                DateTime.UtcNow,
                totalBefore,
                mergedCount,
                stalePruned,
                duplicatesRemoved,
                episodicRetentionPruned,
                semanticAfter,
                Success: true);

            _lastResult = result;
            _logger.LogInformation(
                "Dream consolidation complete: before={Before}, merged={Merged}, stale={Stale}, dupes={Dupes}, retention={Retention}, semantic={Semantic}",
                result.TotalBefore,
                result.EpisodicMergedToSemantic,
                result.StalePruned,
                result.DuplicatesRemoved,
                result.EpisodicRetentionPruned,
                result.SemanticAfter);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dream consolidation failed");
            var failed = new DreamConsolidationResult(
                started,
                DateTime.UtcNow,
                TotalBefore: 0,
                EpisodicMergedToSemantic: 0,
                StalePruned: 0,
                DuplicatesRemoved: 0,
                EpisodicRetentionPruned: 0,
                SemanticAfter: 0,
                Success: false,
                ErrorMessage: ex.Message);
            _lastResult = failed;
            return failed;
        }
    }

    private async Task<(int MergedCount, IReadOnlyList<Guid> DeleteIds)> MergeEpisodicToSemanticAsync(
        IReadOnlyList<HermesMemoryEntry> entries,
        CancellationToken ct)
    {
        var episodic = entries.Where(entry => entry.Kind == MemoryKind.Episodic).ToList();
        if (episodic.Count == 0)
            return (0, Array.Empty<Guid>());

        var merged = 0;
        var deleteIds = new List<Guid>();
        foreach (var group in episodic.GroupBy(entry => entry.RequestFingerprint, StringComparer.Ordinal))
        {
            var clusters = ClusterByMinHash(group.ToList());
            foreach (var cluster in clusters)
            {
                if (cluster.Count < _options.MinEpisodicClusterSize)
                    continue;

                var semantic = BuildSemanticFromCluster(cluster);
                await _store.UpsertAsync(semantic, ct).ConfigureAwait(false);
                deleteIds.AddRange(cluster.Select(entry => entry.Id));
                merged++;
            }
        }

        return (merged, deleteIds);
    }

    private List<List<HermesMemoryEntry>> ClusterByMinHash(IReadOnlyList<HermesMemoryEntry> entries)
    {
        var signatures = entries
            .Select(entry => (Entry: entry, Signature: MinHashSimilarity.ComputeSignature($"{entry.Key} {entry.Summary}")))
            .ToList();

        var clusters = new List<List<HermesMemoryEntry>>();
        var assigned = new bool[signatures.Count];

        for (var i = 0; i < signatures.Count; i++)
        {
            if (assigned[i])
                continue;

            var cluster = new List<HermesMemoryEntry> { signatures[i].Entry };
            assigned[i] = true;

            for (var j = i + 1; j < signatures.Count; j++)
            {
                if (assigned[j])
                    continue;

                var similarity = MinHashSimilarity.EstimateTextSimilarity(
                    $"{signatures[i].Entry.Key} {signatures[i].Entry.Summary}",
                    $"{signatures[j].Entry.Key} {signatures[j].Entry.Summary}");
                if (similarity >= _options.MinHashSimilarityThreshold)
                {
                    cluster.Add(signatures[j].Entry);
                    assigned[j] = true;
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    private static HermesMemoryEntry BuildSemanticFromCluster(IReadOnlyList<HermesMemoryEntry> cluster)
    {
        var anchor = cluster.OrderByDescending(entry => entry.Score).ThenByDescending(entry => entry.CreatedAtUtc).First();
        var summaries = cluster
            .Select(entry => entry.Summary.Trim())
            .Where(summary => summary.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4);
        var mergedSummary = string.Join(" | ", summaries);
        var avgScore = cluster.Average(entry => entry.Score);

        return anchor with
        {
            Id = Guid.NewGuid(),
            Kind = MemoryKind.Semantic,
            Stage = "dream_consolidation",
            Key = $"dream-{ShortKey(anchor.Key)}",
            Summary = mergedSummary,
            Score = Math.Max(avgScore, anchor.Score) + 0.1,
            CreatedAtUtc = DateTime.UtcNow,
            PayloadJson = $"{{\"source\":\"dream_consolidation\",\"merged_from\":{cluster.Count}}}"
        };
    }

    private List<Guid> FindDuplicateIds(IReadOnlyList<HermesMemoryEntry> entries)
    {
        var candidates = entries
            .Where(entry => entry.Kind != MemoryKind.Episodic)
            .Select(entry => (Entry: entry, Signature: MinHashSimilarity.ComputeSignature($"{entry.Key} {entry.Summary}")))
            .OrderByDescending(pair => pair.Entry.Score)
            .ThenByDescending(pair => pair.Entry.CreatedAtUtc)
            .ToList();

        var deleteIds = new List<Guid>();
        for (var i = 0; i < candidates.Count; i++)
        {
            for (var j = i + 1; j < candidates.Count; j++)
            {
                var similarity = MinHashSimilarity.EstimateTextSimilarity(
                    candidates[i].Entry.Summary,
                    candidates[j].Entry.Summary);
                if (similarity >= _options.MinHashSimilarityThreshold)
                    deleteIds.Add(candidates[j].Entry.Id);
            }
        }

        return deleteIds.Distinct().ToList();
    }

    private static string ShortKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "cluster";
        return key.Length <= 24 ? key : key[..24];
    }
}
