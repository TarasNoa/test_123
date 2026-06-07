using System.Net.Http.Json;
using System.Text.Json;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class LocalEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly MemoryEmbeddingOptions _options;
    private readonly ILogger<LocalEmbeddingService> _logger;
    private bool _ollamaAvailable = true;

    public LocalEmbeddingService(
        HttpClient http,
        IOptions<QdrantSyncOptions> options,
        ILogger<LocalEmbeddingService> logger)
    {
        _http = http;
        _options = options.Value.Embeddings;
        _logger = logger;
    }

    public int Dimensions => _options.Dimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!_ollamaAvailable)
            return FallbackEmbed(text);

        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{_options.BaseUrl}/api/embeddings",
                new { model = _options.Model, prompt = TruncateText(text) },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[MemoryEmbedding] Ollama returned {Status}, using fallback", response.StatusCode);
                _ollamaAvailable = false;
                return FallbackEmbed(text);
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
                cancellationToken: ct).ConfigureAwait(false);
            var embeddingArray = doc.RootElement.GetProperty("embedding");

            var result = new float[embeddingArray.GetArrayLength()];
            var i = 0;
            foreach (var element in embeddingArray.EnumerateArray())
                result[i++] = element.GetSingle();

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("[MemoryEmbedding] Ollama unavailable: {Message}. Using fallback.", ex.Message);
                _ollamaAvailable = false;
            }

            return FallbackEmbed(text);
        }
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var results = new float[texts.Count][];
        using var semaphore = new SemaphoreSlim(4, 4);
        await Parallel.ForEachAsync(
            texts.Select((text, index) => (text, index)),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (item, innerCt) =>
            {
                await semaphore.WaitAsync(innerCt).ConfigureAwait(false);
                try
                {
                    results[item.index] = await EmbedAsync(item.text, innerCt).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ConfigureAwait(false);

        return results;
    }

    internal float[] FallbackEmbed(string text)
    {
        var vec = new float[_options.Dimensions];
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text));

        for (var i = 0; i < _options.Dimensions; i++)
            vec[i] = (bytes[i % 32] / 255f) * 2f - 1f;

        var norm = MathF.Sqrt(vec.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < vec.Length; i++)
                vec[i] /= norm;
        }

        return vec;
    }

    private static string TruncateText(string text, int maxChars = 2000) =>
        text.Length <= maxChars ? text : text[..maxChars];
}
