namespace Libr4.IDE.Application.CodeSearch;

/// <summary>
/// Provides dense vector embeddings for semantic code search.
/// Supports multiple backends: Ollama (local), OpenAI, AlibabaCloud.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Embed a single text. Returns float[] of configured dimensions.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Batch embed multiple texts efficiently.</summary>
    Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);

    /// <summary>Configured embedding dimensions (e.g. 384, 768, 1536).</summary>
    int Dimensions { get; }
}
