using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class QdrantSyncHermesMemoryStore : IHermesMemoryStore
{
    private readonly IHermesMemoryStore _inner;
    private readonly IHermesVectorSyncService _sync;

    public QdrantSyncHermesMemoryStore(IHermesMemoryStore inner, IHermesVectorSyncService sync)
    {
        _inner = inner;
        _sync = sync;
    }

    public Task EnsureSchemaAsync(CancellationToken ct = default) =>
        _inner.EnsureSchemaAsync(ct);

    public async Task UpsertAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        await _inner.UpsertAsync(entry, ct).ConfigureAwait(false);
        await _sync.SyncEntryAsync(entry, ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<HermesMemoryRetrievalResult>> RetrieveAsync(HermesMemoryQuery query, CancellationToken ct = default) =>
        _inner.RetrieveAsync(query, ct);

    public Task<int> PruneExpiredEpisodicAsync(CancellationToken ct = default) =>
        _inner.PruneExpiredEpisodicAsync(ct);

    public Task PruneByTokenBudgetAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct = default) =>
        _inner.PruneByTokenBudgetAsync(requestFingerprint, maxTokenBudget, ct);

    public Task<IReadOnlyList<HermesMemorySearchHit>> SearchSummariesAsync(string query, int limit = 25, CancellationToken ct = default) =>
        _inner.SearchSummariesAsync(query, limit, ct);

    public Task<IReadOnlyList<HermesMemoryEntry>> ListAllAsync(CancellationToken ct = default) =>
        _inner.ListAllAsync(ct);

    public async Task<int> DeleteByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var all = await _inner.ListAllAsync(ct).ConfigureAwait(false);
        var toRemove = all.Where(entry => ids.Contains(entry.Id)).ToList();
        var deleted = await _inner.DeleteByIdsAsync(ids, ct).ConfigureAwait(false);

        foreach (var entry in toRemove)
            await _sync.RemoveEntryAsync(entry, ct).ConfigureAwait(false);

        return deleted;
    }
}
