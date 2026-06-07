using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public sealed class CognitiveSyncHermesMemoryStore : IHermesMemoryStore
{
    private readonly IHermesMemoryStore _inner;
    private readonly ICognitiveMemoryBridge _bridge;

    public CognitiveSyncHermesMemoryStore(IHermesMemoryStore inner, ICognitiveMemoryBridge bridge)
    {
        _inner = inner;
        _bridge = bridge;
    }

    public Task EnsureSchemaAsync(CancellationToken ct = default) =>
        _inner.EnsureSchemaAsync(ct);

    public async Task UpsertAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        await _inner.UpsertAsync(entry, ct).ConfigureAwait(false);
        await _bridge.SyncFromHermesEntryAsync(entry, ct).ConfigureAwait(false);
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

    public Task<int> DeleteByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default) =>
        _inner.DeleteByIdsAsync(ids, ct);
}
