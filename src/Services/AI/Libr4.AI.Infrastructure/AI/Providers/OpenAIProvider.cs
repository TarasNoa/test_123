using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI.Providers;

/// <summary>
/// OpenAI API provider implementation.
/// Supports GPT-4, GPT-3.5, embeddings, and text analysis.
/// </summary>
public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly string _apiKey;

    public string ProviderName => "OpenAI";

    public OpenAIProvider(IConfiguration configuration, ILogger<OpenAIProvider> logger, HttpClient? httpClient = null)
    {
        _logger = logger;
        _apiKey = configuration["AI:OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("AI:OpenAI:ApiKey not configured");
        
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        try
        {
            var request = new
            {
                model = model ?? "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt ?? "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            _logger.LogDebug("Sending completion request to OpenAI");
            
            var response = await _httpClient.PostAsJsonAsync("chat/completions", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            
            if (result?.Choices?.FirstOrDefault()?.Message?.Content is string content)
            {
                _logger.LogDebug("OpenAI completion successful, tokens used: {Tokens}", result.Usage?.TotalTokens ?? 0);
                return content;
            }

            throw new InvalidOperationException("Empty response from OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI completion failed");
            throw new AIProviderException("OpenAI API error", ex);
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        try
        {
            var request = new
            {
                model = model ?? "text-embedding-3-small",
                input = text
            };

            _logger.LogDebug("Generating embedding with OpenAI");
            
            var response = await _httpClient.PostAsJsonAsync("embeddings", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
            
            if (result?.Data?.FirstOrDefault()?.Embedding is float[] embedding)
            {
                return JsonSerializer.Serialize(embedding);
            }

            throw new InvalidOperationException("Empty embedding response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI embedding failed");
            throw new AIProviderException("OpenAI embedding error", ex);
        }
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var analysisPrompt = analysisType.ToLower() switch
        {
            "sentiment" => $"Analyze the sentiment of this text. Return ONLY a JSON object with 'sentiment' (positive/negative/neutral) and 'confidence' (0-1):\n\n{text}",
            "entities" => $"Extract named entities from this text. Return as JSON array of objects with 'entity', 'type', and 'confidence':\n\n{text}",
            "summary" => $"Summarize this text in 2-3 sentences:\n\n{text}",
            "keywords" => $"Extract 5-10 keywords from this text as JSON array:\n\n{text}",
            _ => $"Analyze this text for {analysisType}:\n\n{text}"
        };

        try
        {
            var result = await GenerateCompletionAsync(analysisPrompt, 
                "You are a precise text analysis assistant. Always return valid JSON.", 
                model ?? "gpt-3.5-turbo");
            
            // Try to extract JSON if present
            if (result.Contains('{') && result.Contains('}'))
            {
                var start = result.IndexOf('{');
                var end = result.LastIndexOf('}') + 1;
                return result[start..end];
            }

            return $"{{\"result\": \"{result.Replace("\"", "\\\"")}\"}}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI text analysis failed for type {Type}", analysisType);
            throw new AIProviderException($"Text analysis failed: {analysisType}", ex);
        }
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
    {
        return await GenerateCompletionAsync(message, systemPrompt, model);
    }
}

/// <summary>
/// OpenAI API response models
/// </summary>
public class OpenAIChatResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<OpenAIChoice>? Choices { get; set; }
    public OpenAIUsage? Usage { get; set; }
}

public class OpenAIChoice
{
    public int Index { get; set; }
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class OpenAIMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class OpenAIEmbeddingResponse
{
    public string? Object { get; set; }
    public List<OpenAIEmbeddingData>? Data { get; set; }
    public string? Model { get; set; }
    public OpenAIUsage? Usage { get; set; }
}

public class OpenAIEmbeddingData
{
    public string? Object { get; set; }
    public float[]? Embedding { get; set; }
    public int Index { get; set; }
}

/// <summary>
/// Custom exception for AI provider errors
/// </summary>
public class AIProviderException : Exception
{
    public AIProviderException(string message, Exception? innerException = null) 
        : base(message, innerException) { }
}
