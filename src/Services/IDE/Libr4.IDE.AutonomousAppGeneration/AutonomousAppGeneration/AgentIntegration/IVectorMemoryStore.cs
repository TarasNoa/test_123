namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// P2-2: Vector memory store interface for semantic (embedding-based) retrieval.
/// Default implementation uses in-process cosine similarity over stored float arrays;
/// production deployments should swap to pgvector or Qdrant via the same interface.
/// </summary>
public interface IVectorMemoryStore
{
    /// <summary>
    /// Upserts a vector record keyed by <paramref name="id"/>.
    /// If a record with the same id exists it is replaced.
    /// </summary>
    Task UpsertAsync(VectorRecord record, CancellationToken ct = default);

    /// <summary>
    /// Queries up to <paramref name="topK"/> records whose embedding is closest to
    /// <paramref name="queryEmbedding"/> (cosine similarity), optionally filtered by
    /// <paramref name="collectionId"/> and <paramref name="minScore"/>.
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        string? collectionId = null,
        int topK = 10,
        double minScore = 0.0,
        CancellationToken ct = default);

    /// <summary>Deletes all records belonging to <paramref name="collectionId"/>.</summary>
    Task DeleteCollectionAsync(string collectionId, CancellationToken ct = default);

    /// <summary>Deletes a single record by id within <paramref name="collectionId"/>.</summary>
    Task DeleteAsync(string id, string collectionId, CancellationToken ct = default);
}

/// <summary>
/// A single embeddable unit stored in the vector store.
/// </summary>
public sealed record VectorRecord(
    string Id,
    string CollectionId,
    float[] Embedding,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// A result from a vector similarity search.
/// </summary>
public sealed record VectorSearchResult(
    VectorRecord Record,
    double Score);
