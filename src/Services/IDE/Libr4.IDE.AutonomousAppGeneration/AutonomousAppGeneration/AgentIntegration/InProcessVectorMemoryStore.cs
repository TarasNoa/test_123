using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// P2-2: In-process vector memory store backed by a concurrent dictionary.
/// Uses cosine similarity for ranking. Suitable for single-host development/test;
/// swap to a pgvector or Qdrant adapter for production multi-host deployments.
/// </summary>
public sealed class InProcessVectorMemoryStore : IVectorMemoryStore
{
    private readonly ConcurrentDictionary<string, VectorRecord> _store = new(StringComparer.Ordinal);

    public Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        _store[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        string? collectionId = null,
        int topK = 10,
        double minScore = 0.0,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        var candidates = _store.Values
            .Where(r => collectionId is null ||
                        string.Equals(r.CollectionId, collectionId, StringComparison.Ordinal));

        var results = candidates
            .Select(r => new VectorSearchResult(r, CosineSimilarity(queryEmbedding, r.Embedding)))
            .Where(s => s.Score >= minScore)
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public Task DeleteCollectionAsync(string collectionId, CancellationToken ct = default)
    {
        var keys = _store
            .Where(kv => string.Equals(kv.Value.CollectionId, collectionId, StringComparison.Ordinal))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in keys)
            _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Computes cosine similarity between two float vectors.
    /// Returns 0 if either vector is zero-length or dimension-mismatched.
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
            return 0.0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom < 1e-10 ? 0.0 : dot / denom;
    }
}
