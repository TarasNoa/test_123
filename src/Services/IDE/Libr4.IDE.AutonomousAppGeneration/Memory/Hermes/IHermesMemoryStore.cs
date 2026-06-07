namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public interface IHermesMemoryStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task UpsertAsync(HermesMemoryEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<HermesMemoryRetrievalResult>> RetrieveAsync(HermesMemoryQuery query, CancellationToken ct = default);

    /// <summary>Delete L0 episodic rows older than configured retention.</summary>
    Task<int> PruneExpiredEpisodicAsync(CancellationToken ct = default);

    /// <summary>Keep highest-scored rows within token budget for a fingerprint.</summary>
    Task PruneByTokenBudgetAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct = default);

    /// <summary>FTS5 search across memory summaries and keys.</summary>
    Task<IReadOnlyList<HermesMemorySearchHit>> SearchSummariesAsync(string query, int limit = 25, CancellationToken ct = default);

    Task<IReadOnlyList<HermesMemoryEntry>> ListAllAsync(CancellationToken ct = default);

    Task<int> DeleteByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
}

public sealed record HermesMemorySearchHit(
    Guid RunId,
    string Kind,
    string Key,
    string Snippet,
    double Score);
