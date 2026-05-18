using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.AI.Application.Abstractions;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.AI.Infrastructure.LLM;

/// <summary>
/// Ollama LLM Provider - uses Ollama's OpenAI-compatible API
/// Ollama exposes /v1/chat/completions endpoint that is compatible with OpenAI format
/// </summary>
public class OllamaLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLLMProvider> _logger;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public string Name => "Ollama";

    public OllamaLLMProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaLLMProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "qwen3:8b";
        
        // Ollama API is OpenAI-compatible at /v1 path
        _httpClient.BaseAddress = new Uri($"{_baseUrl}/v1/");
        // No API key needed for local Ollama
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<ChatCompletionResponse>> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = request.Model ?? _defaultModel;
            
            _logger.LogInformation("Ollama: Calling model {Model} with {MessageCount} messages", 
                model, request.Messages.Count);

            var ollamaRequest = new OllamaChatRequest
            {
                Model = model,
                Messages = request.Messages.Select(m => new OllamaMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                ollamaRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama API error: {Error}", error);
                return Result.Failure<ChatCompletionResponse>(
                    Error.Failure("Ollama.Error", $"API error: {response.StatusCode} - {error}"));
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            
            if (result?.Choices == null || result.Choices.Count == 0)
                return Result.Failure<ChatCompletionResponse>(
                    Error.Failure("Ollama.Empty", "Empty response from Ollama"));

            var choice = result.Choices[0];
            return Result.Success(new ChatCompletionResponse(
                choice.Message?.Content ?? "",
                result.Usage?.TotalTokens ?? estimateTokens(request.Messages),
                choice.FinishReason));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama generation failed");
            return Result.Failure<ChatCompletionResponse>(
                Error.Failure("Ollama.Exception", ex.Message));
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _defaultModel;
        
        var ollamaRequest = new OllamaChatRequest
        {
            Model = model,
            Messages = request.Messages.Select(m => new OllamaMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            Temperature = request.Temperature,
            Stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            ollamaRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ollama streaming error: {Status}", response.StatusCode);
            yield break;
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var data = line.Substring(6);
            if (data == "[DONE]") break;

            string? delta = null;
            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(data);
                delta = chunk?.Choices?[0]?.Delta?.Content;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse streaming chunk");
            }
            if (!string.IsNullOrEmpty(delta)) yield return delta;
        }
    }

    private static int estimateTokens(List<ChatMessage> messages)
    {
        // Rough estimation: ~4 characters per token
        var totalChars = messages.Sum(m => m.Content.Length);
        return totalChars / 4;
    }

    // OpenAI-compatible request/response models for Ollama
    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OllamaChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OllamaUsage? Usage { get; set; }
    }

    private class OllamaChoice
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class OllamaUsage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class OllamaStreamChunk
    {
        [JsonPropertyName("choices")]
        public List<OllamaStreamChoice>? Choices { get; set; }
    }

    private class OllamaStreamChoice
    {
        [JsonPropertyName("delta")]
        public OllamaStreamDelta? Delta { get; set; }
    }

    private class OllamaStreamDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
