namespace Libr4.Matching.Application.Abstractions;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
    Task<float> SimilarityAsync(float[] vectorA, float[] vectorB, CancellationToken ct = default);
}
