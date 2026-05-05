namespace Libr4.AI.Application.Memory;

/// <summary>
/// Service for generating text embeddings (vector representations).
/// Supports multiple embedding providers (OpenAI, Azure, local models).
/// </summary>
public interface IEmbeddingsService
{
    /// <summary>
    /// Generate embedding for a single text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generate embeddings for multiple texts (batch processing).
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        string? model = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get the dimension size of embeddings from this service.
    /// </summary>
    int GetEmbeddingDimension();
}

/// <summary>
/// Configuration for embeddings service.
/// </summary>
public sealed record EmbeddingsOptions
{
    public string Provider { get; init; } = "openai"; // openai, azure, local
    public string Model { get; init; } = "text-embedding-3-small";
    public int Dimensions { get; init; } = 1536;
    public string? ApiKey { get; init; }
    public string? Endpoint { get; init; } // For Azure or custom endpoints
    public int MaxBatchSize { get; init; } = 100;
    public int MaxRetries { get; init; } = 3;
}
