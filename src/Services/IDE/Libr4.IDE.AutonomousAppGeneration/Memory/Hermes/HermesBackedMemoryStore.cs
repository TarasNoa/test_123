using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

/// <summary>
/// Routes legacy <see cref="IMemoryStore"/> calls through the wired <see cref="IHermesMemoryStore"/>
/// decorator chain (cognitive + optional qdrant sync).
/// </summary>
public sealed class HermesBackedMemoryStore : IMemoryStore
{
    private readonly IHermesMemoryStore _store;

    public HermesBackedMemoryStore(IHermesMemoryStore store) => _store = store;

    public Task IngestAsync(MemoryRecord record, CancellationToken ct) =>
        _store.UpsertAsync(HermesMemoryMapper.ToHermesEntry(record), ct);

    public async Task<IReadOnlyList<MemoryRetrievalResult>> RetrieveAsync(MemoryQuery query, CancellationToken ct)
    {
        var results = await _store.RetrieveAsync(
            new HermesMemoryQuery(query.RequestFingerprint, query.Keyword, query.TopK, query.Kinds),
            ct).ConfigureAwait(false);

        return results
            .Select(result => new MemoryRetrievalResult(
                HermesMemoryMapper.ToMemoryRecord(result.Entry),
                result.RetrievalReason,
                result.RelevanceScore))
            .ToList();
    }

    public Task PruneAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct) =>
        _store.PruneByTokenBudgetAsync(requestFingerprint, maxTokenBudget, ct);
}
