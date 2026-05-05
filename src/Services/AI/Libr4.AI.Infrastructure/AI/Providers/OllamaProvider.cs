using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI.Providers;

/// <summary>
/// Provider for Ollama (local LLM runtime).
/// Exposes an OpenAI-compatible API at http://localhost:11434/api by default.
/// No API key is required. Models are identified by their name in Ollama
/// (e.g. "qwen35b" after `ollama create qwen35b -f Modelfile`).
/// </summary>
public class OllamaProvider : IAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public string ProviderName => "Ollama";

    public OllamaProvider(
        IConfiguration configuration,
        ILogger<OllamaProvider> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _endpoint = _configuration["AI:Ollama:Endpoint"]
                    ?? "http://localhost:11434/api";
        // Local inference can take a while; be generous.
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        var modelName = model
            ?? _configuration["AI:Ollama:DefaultModel"]
            ?? "qwen35b";

        _logger.LogInformation("Ollama: Calling model {Model} with prompt length {PromptLength}",
            modelName, prompt?.Length ?? 0);

        var requestBody = new
        {
            model = modelName,
            prompt = prompt,
            system = systemPrompt ?? "You are a helpful assistant.",
            temperature = 0.3,
            top_p = 0.9,
            stream = false
        };

        var contentBuilder = new StringBuilder();

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("Ollama: Request body: {RequestBody}", json);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/generate")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama: API call failed with status {Status}. Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Ollama API call failed: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (responseJson.TryGetProperty("response", out var responseEl) && responseEl.ValueKind == JsonValueKind.String)
                {
                    contentBuilder.Append(responseEl.GetString());
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Ollama: Failed to parse response as JSON, using raw content");
                contentBuilder.Append(responseContent);
            }

            var content = contentBuilder.ToString();

            if (content.Length == 0)
            {
                throw new HttpRequestException("Ollama: Response produced no content.");
            }

            _logger.LogInformation(
                "Ollama: Response with length {Length}",
                content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama: Exception during API call");
            throw;
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var modelName = model
            ?? _configuration["AI:Ollama:EmbeddingModel"]
            ?? throw new InvalidOperationException(
                "Ollama embeddings require AI:Ollama:EmbeddingModel to be configured.");

        var requestBody = new { model = modelName, prompt = text };
        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Ollama embeddings failed: {response.StatusCode} - {errorContent}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var prompt = $"Analyze the following text. Analysis type: {analysisType}\n\nText:\n{text}";
        return await GenerateCompletionAsync(prompt, null, model);
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
    {
        return await GenerateCompletionAsync(message, systemPrompt, model);
    }
}
