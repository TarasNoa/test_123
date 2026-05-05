using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.AI.Application.Abstractions;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.AI.Infrastructure.LLM;

public class OpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly string _apiKey;

    public string Name => "OpenAI";

    public OpenAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrEmpty(_apiKey));
    }

    public async Task<Result<ChatCompletionResponse>> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var openaiRequest = new OpenAIChatRequest
            {
                Model = request.Model,
                Messages = request.Messages.Select(m => new OpenAIMessage
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
                openaiRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API error: {Error}", error);
                return Result.Failure<ChatCompletionResponse>(
                    Error.Failure("OpenAI.Error", $"API error: {response.StatusCode}"));
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(cancellationToken);
            
            if (result?.Choices == null || result.Choices.Count == 0)
                return Result.Failure<ChatCompletionResponse>(
                    Error.Failure("OpenAI.Empty", "Empty response from OpenAI"));

            var choice = result.Choices[0];
            return Result.Success(new ChatCompletionResponse(
                choice.Message?.Content ?? "",
                result.Usage?.TotalTokens ?? 0,
                choice.FinishReason));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI generation failed");
            return Result.Failure<ChatCompletionResponse>(
                Error.Failure("OpenAI.Exception", ex.Message));
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openaiRequest = new OpenAIChatRequest
        {
            Model = request.Model,
            Messages = request.Messages.Select(m => new OpenAIMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            Temperature = request.Temperature,
            Stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            openaiRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            yield break;

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
                var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data);
                delta = chunk?.Choices?[0]?.Delta?.Content;
            }
            catch { /* skip invalid chunks */ }
            if (!string.IsNullOrEmpty(delta)) yield return delta;
        }
    }

    private class OpenAIChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class OpenAIUsage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class OpenAIStreamChunk
    {
        [JsonPropertyName("choices")]
        public List<OpenAIStreamChoice>? Choices { get; set; }
    }

    private class OpenAIStreamChoice
    {
        [JsonPropertyName("delta")]
        public OpenAIStreamDelta? Delta { get; set; }
    }

    private class OpenAIStreamDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
