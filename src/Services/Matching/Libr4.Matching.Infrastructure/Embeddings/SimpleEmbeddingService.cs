using Libr4.Matching.Application.Abstractions;

namespace Libr4.Matching.Infrastructure.Embeddings;

public sealed class SimpleEmbeddingService : IEmbeddingService
{
    private const int EmbeddingDimension = 768;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var embedding = GenerateEmbedding(text);
        return Task.FromResult(embedding);
    }

    public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var result = texts.Select(GenerateEmbedding).ToArray();
        return Task.FromResult(result);
    }

    public Task<float> SimilarityAsync(float[] vectorA, float[] vectorB, CancellationToken ct = default)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < vectorA.Length; i++)
        {
            dot += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        var similarity = dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB) + 1e-6f);
        return Task.FromResult(similarity);
    }

    private static float[] GenerateEmbedding(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
        var seed = BitConverter.ToInt32(bytes, 0);
        var random = new Random(seed);
        var embedding = new float[EmbeddingDimension];
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }
        // Normalize
        var norm = MathF.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < EmbeddingDimension; i++)
                embedding[i] /= norm;
        }
        return embedding;
    }
}
