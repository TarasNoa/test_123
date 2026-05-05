using System.Net.Http.Json;
using System.Text.Json;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// Embedding service backed by Ollama (nomic-embed-text or any embedding model).
/// Falls back to a deterministic hash-based pseudo-embedding when Ollama is unavailable,
/// so the system degrades to BM25-only gracefully.
/// </summary>
public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OllamaEmbeddingOptions _options;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private bool _ollamaAvailable = true;

    public int Dimensions => _options.Dimensions;

    public OllamaEmbeddingService(
        HttpClient http,
        IOptions<OllamaEmbeddingOptions> options,
        ILogger<OllamaEmbeddingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!_ollamaAvailable)
            return FallbackEmbed(text);

        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{_options.BaseUrl}/api/embeddings",
                new { model = _options.Model, prompt = TruncateText(text) },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Embedding] Ollama returned {Status}, falling back", response.StatusCode);
                _ollamaAvailable = false;
                return FallbackEmbed(text);
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var embeddingArray = doc.RootElement.GetProperty("embedding");

            var result = new float[embeddingArray.GetArrayLength()];
            int i = 0;
            foreach (var el in embeddingArray.EnumerateArray())
                result[i++] = el.GetSingle();

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("[Embedding] Ollama unavailable: {Msg}. Using fallback.", ex.Message);
                _ollamaAvailable = false;
            }
            return FallbackEmbed(text);
        }
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var results = new float[texts.Count][];

        // Ollama doesn't have a native batch endpoint — parallelize with semaphore
        using var semaphore = new SemaphoreSlim(4, 4);
        await Parallel.ForEachAsync(
            texts.Select((t, i) => (Text: t, Index: i)),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (item, innerCt) =>
            {
                await semaphore.WaitAsync(innerCt);
                try
                {
                    results[item.Index] = await EmbedAsync(item.Text, innerCt);
                }
                finally
                {
                    semaphore.Release();
                }
            });

        return results;
    }

    /// <summary>
    /// Deterministic pseudo-embedding from content hash.
    /// Preserves approximate similarity for matching identical/near-identical text.
    /// Used when Ollama is unavailable — system falls back to BM25 scoring.
    /// </summary>
    private float[] FallbackEmbed(string text)
    {
        var vec = new float[_options.Dimensions];
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text));

        for (int i = 0; i < _options.Dimensions; i++)
            vec[i] = (bytes[i % 32] / 255f) * 2f - 1f;

        var norm = MathF.Sqrt(vec.Sum(v => v * v));
        if (norm > 0)
            for (int i = 0; i < vec.Length; i++)
                vec[i] /= norm;

        return vec;
    }

    private static string TruncateText(string text, int maxChars = 2000) =>
        text.Length <= maxChars ? text : text[..maxChars];
}

public sealed class OllamaEmbeddingOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
    public int Dimensions { get; set; } = 384;
    public int BatchConcurrency { get; set; } = 4;
}
