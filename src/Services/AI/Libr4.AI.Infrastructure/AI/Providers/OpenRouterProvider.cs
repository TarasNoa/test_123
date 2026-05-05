using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI.Providers;

public class OpenRouterProvider : IAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;

    public string ProviderName => "OpenRouter";

    public OpenRouterProvider(IConfiguration configuration, ILogger<OpenRouterProvider> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = _configuration["AI:OpenRouter:ApiKey"] ?? throw new InvalidOperationException("AI:OpenRouter:ApiKey not configured");
        _endpoint = _configuration["AI:OpenRouter:Endpoint"] ?? "https://openrouter.ai/api/v1";
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://libr4.com");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Libr4");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        var modelName = model ?? _configuration["AI:OpenRouter:DefaultModel"] ?? "anthropic/claude-3.5-sonnet";
        
        _logger.LogInformation("OpenRouter: Calling model {Model} with prompt length {PromptLength}", modelName, prompt?.Length ?? 0);

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 8192
        };

        try
        {
            _logger.LogDebug("OpenRouter: Request body: {RequestBody}", JsonSerializer.Serialize(requestBody));

            var response = await _httpClient.PostAsync(
                $"{_endpoint}/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));

            _logger.LogInformation("OpenRouter: Response status {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenRouter: API call failed with status {StatusCode}. Response: {Response}", response.StatusCode, errorContent);
                throw new HttpRequestException($"OpenRouter API call failed: {response.StatusCode} - {errorContent}");
            }

            // Use ReadAsStreamAsync to handle chunked encoding properly
            await using var stream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
            var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

            _logger.LogInformation("OpenRouter: Successfully generated response with length {Length}", content?.Length ?? 0);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter: Exception during API call");
            throw;
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var modelName = model ?? _configuration["AI:OpenRouter:EmbeddingModel"] ?? "openai/text-embedding-ada-002";
        
        var requestBody = new
        {
            model = modelName,
            input = text
        };

        var response = await _httpClient.PostAsync(
            $"{_endpoint}/embeddings",
            new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        return result.GetProperty("data")[0].GetProperty("embedding").ToString() ?? string.Empty;
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var systemPrompt = analysisType switch
        {
            "sentiment" => "You are a sentiment analysis expert. Analyze the text and return sentiment (positive/negative/neutral) with confidence score (0-1) in JSON format.",
            "complexity" => "You are a task complexity analyst. Analyze the task description and return complexity score (1-10), estimated hours, and required skills in JSON format.",
            "skills" => "You are a skills extraction expert. Extract all technical and soft skills from the text and return as a JSON array.",
            "risk" => "You are a risk assessment expert. Analyze the project/task and return risk level (low/medium/high) with explanation in JSON format.",
            _ => "You are a helpful assistant. Analyze the text according to the request."
        };

        var prompt = $"Analyze the following text for {analysisType}: {text}";
        return await GenerateCompletionAsync(prompt, systemPrompt, model);
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
    {
        return await GenerateCompletionAsync(message, systemPrompt, model);
    }
}
