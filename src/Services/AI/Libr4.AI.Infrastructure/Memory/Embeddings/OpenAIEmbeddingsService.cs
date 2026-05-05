/*
using Microsoft.Extensions.Logging;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.AI.Application.Memory;

namespace Libr4.AI.Infrastructure.Memory.Embeddings;

/// <summary>
/// OpenAI-based embeddings service.
/// Supports text-embedding-3-small, text-embedding-3-large, and ada-002.
/// </summary>
public sealed class OpenAIEmbeddingsService : IEmbeddingsService
{
    private readonly ApiKeyCredential _credential;
    private readonly Uri _endpoint;
    private readonly string _model;
    private readonly int _dimensions;
    private readonly ILogger<OpenAIEmbeddingsService> _logger;
    private readonly HttpClient _httpClient;

    public OpenAIEmbeddingsService(
        string apiKey,
        string? endpoint = null,
        string model = "text-embedding-3-small",
        ILogger<OpenAIEmbeddingsService>? logger = null,
        HttpClient? httpClient = null)
    {
        _credential = new ApiKeyCredential(apiKey);
        _endpoint = endpoint != null 
            ? new Uri(endpoint) 
            : new Uri("https://api.openai.com/v1/");
        _model = model;
        _dimensions = model switch
        {
            "text-embedding-3-small" => 1536,
            "text-embedding-3-large" => 3072,
            "text-embedding-ada-002" => 1536,
            _ => 1536
        };
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<OpenAIEmbeddingsService>();
        _httpClient = httpClient ?? new HttpClient();
    }

    public int GetEmbeddingDimension() => _dimensions;

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken ct = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, model, ct);
        return results.FirstOrDefault() ?? Array.Empty<float>();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        string? model = null,
        CancellationToken ct = default)
    {
        var targetModel = model ?? _model;
        var embeddings = new List<float[]>();

        // Process in batches of 100 (OpenAI limit)
        const int batchSize = 100;
        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var batchEmbeddings = await GenerateBatchAsync(batch, targetModel, ct);
            embeddings.AddRange(batchEmbeddings);
        }

        return embeddings;
    }

    private async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        string model,
        CancellationToken ct)
    {
        // Filter and truncate texts
        var validTexts = texts
            .Select(t => t?.Trim() ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => TruncateForTokenLimit(t, 8192))
            .ToList();

        if (validTexts.Count == 0)
        {
            return texts.Select(_ => new float[_dimensions]).ToList();
        }

        var request = new
        {
            model = model,
            input = validTexts,
            encoding_format = "float"
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_endpoint, "embeddings"));
        requestMessage.Content = content;
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credential.ToString());

        var response = await _httpClient.SendAsync(requestMessage, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI embeddings API error: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"OpenAI embeddings API error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OpenAIEmbeddingsResponse>(responseJson);

        if (result?.Data == null)
        {
            throw new InvalidOperationException("Failed to parse embeddings response");
        }

        // Map results back to original order (handle filtered texts)
        var embeddingMap = result.Data.ToDictionary(
            d => d.Index, 
            d => d.Embedding);

        var embeddings = new List<float[]>();
        int validIndex = 0;
        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                embeddings.Add(new float[_dimensions]);
            }
            else
            {
                embeddings.Add(embeddingMap[validIndex++]);
            }
        }

        _logger.LogDebug("Generated {Count} embeddings using model {Model}", 
            embeddings.Count, model);

        return embeddings;
    }

    private static string TruncateForTokenLimit(string text, int maxTokens)
    {
        // Rough approximation: ~4 characters per token
        var maxChars = maxTokens * 4;
        if (text.Length <= maxChars) return text;
        
        return text.Substring(0, maxChars) + "...";
    }

    // OpenAI API response models
    private sealed class OpenAIEmbeddingsResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = new();
        
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";
        
        [JsonPropertyName("usage")]
        public UsageInfo Usage { get; set; } = new();
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
        
        [JsonPropertyName("index")]
        public int Index { get; set; }
        
        [JsonPropertyName("object")]
        public string Object { get; set; } = "";
    }

    private sealed class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}

/// <summary>
/// Azure OpenAI embeddings service.
/// </summary>
public sealed class AzureOpenAIEmbeddingsService : IEmbeddingsService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly int _dimensions;
    private readonly ILogger<AzureOpenAIEmbeddingsService> _logger;
    private readonly HttpClient _httpClient;

    public AzureOpenAIEmbeddingsService(
        string endpoint,
        string apiKey,
        string deploymentName,
        int dimensions = 1536,
        ILogger<AzureOpenAIEmbeddingsService>? logger = null,
        HttpClient? httpClient = null)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _dimensions = dimensions;
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AzureOpenAIEmbeddingsService>();
        _httpClient = httpClient ?? new HttpClient();
    }

    public int GetEmbeddingDimension() => _dimensions;

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken ct = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, model, ct);
        return results.FirstOrDefault() ?? Array.Empty<float>();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        string? model = null,
        CancellationToken ct = default)
    {
        const int batchSize = 96; // Azure limit is typically 96
        var embeddings = new List<float[]>();

        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var batchEmbeddings = await GenerateBatchAsync(batch, ct);
            embeddings.AddRange(batchEmbeddings);
        }

        return embeddings;
    }

    private async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct)
    {
        var validTexts = texts
            .Select(t => t?.Trim() ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        if (validTexts.Count == 0)
        {
            return texts.Select(_ => new float[_dimensions]).ToList();
        }

        var request = new
        {
            input = validTexts
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        var url = $"{_endpoint}/openai/deployments/{_deploymentName}/embeddings?api-version=2024-02-01";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = content;
        requestMessage.Headers.Add("api-key", _apiKey);

        var response = await _httpClient.SendAsync(requestMessage, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Azure OpenAI embeddings API error: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Azure OpenAI embeddings API error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<AzureEmbeddingsResponse>(responseJson);

        if (result?.Data == null)
        {
            throw new InvalidOperationException("Failed to parse embeddings response");
        }

        var embeddingMap = result.Data.ToDictionary(d => d.Index, d => d.Embedding);

        var embeddings = new List<float[]>();
        int validIndex = 0;
        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                embeddings.Add(new float[_dimensions]);
            }
            else
            {
                embeddings.Add(embeddingMap[validIndex++]);
            }
        }

        return embeddings;
    }

    private sealed class AzureEmbeddingsResponse
    {
        [JsonPropertyName("data")]
        public List<AzureEmbeddingData> Data { get; set; } = new();
    }

    private sealed class AzureEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
        
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
*/
