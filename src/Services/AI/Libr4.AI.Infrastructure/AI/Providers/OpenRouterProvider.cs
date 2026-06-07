using System.Net;
using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
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
        _endpoint = (_configuration["AI:OpenRouter:Endpoint"] ?? "https://openrouter.ai/api/v1").TrimEnd('/');
        var timeoutMinutes = configuration.GetValue("AI:OpenRouter:TimeoutMinutes", 15);
        _httpClient.Timeout = TimeSpan.FromMinutes(Math.Clamp(timeoutMinutes, 5, 30));
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        var modelName = model ?? _configuration["AI:OpenRouter:DefaultModel"] ?? "anthropic/claude-3.5-sonnet";
        var disableReasoning = _configuration.GetValue("AI:OpenRouter:DisableReasoning", true);
        var useStreaming = LlmCallPreferenceContext.CurrentPreferences?.DisableStreaming == true
            ? false
            : _configuration.GetValue("AI:OpenRouter:UseStreaming", true);

        // DeepSeek V4 reasoning streams can stall the SSE reader indefinitely; prefer one-shot JSON.
        if (useStreaming
            && disableReasoning
            && modelName.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            useStreaming = false;
        }

        _logger.LogInformation(
            "OpenRouter: Calling model {Model} with prompt length {PromptLength}, stream={Stream}",
            modelName,
            prompt?.Length ?? 0,
            useStreaming);

        try
        {
            if (!useStreaming)
                return await GenerateNonStreamingAsync(prompt, systemPrompt, modelName).ConfigureAwait(false);

            try
            {
                return await GenerateStreamingAsync(prompt, systemPrompt, modelName).ConfigureAwait(false);
            }
            catch (Exception streamEx) when (IsPrematureStreamEnd(streamEx))
            {
                _logger.LogWarning(
                    streamEx,
                    "OpenRouter: stream ended prematurely for {Model}; falling back to non-streaming",
                    modelName);
                return await GenerateNonStreamingAsync(prompt, systemPrompt, modelName).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter: Exception during API call (model={Model})", modelName);
            throw;
        }
    }

    private async Task<string> GenerateStreamingAsync(string? prompt, string? systemPrompt, string modelName)
    {
        using var request = BuildRequest(prompt, systemPrompt, modelName, stream: true);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        var errorBody = !response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
            : null;

        _logger.LogInformation("OpenRouter: Response status {StatusCode}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenRouter: API call failed with status {StatusCode}. Response: {Response}",
                response.StatusCode,
                errorBody);
            throw new HttpRequestException($"OpenRouter API call failed: {response.StatusCode} - {errorBody}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var contentBuilder = new StringBuilder();
        var reasoningBuilder = new StringBuilder();
        var linesWithoutAnyContent = 0;
        var maxLinesWithoutContent = Math.Clamp(
            _configuration.GetValue("AI:OpenRouter:MaxSseLinesWithoutContent", 6000),
            500,
            50000);
        var maxReasoningWithoutContent = Math.Clamp(
            _configuration.GetValue("AI:OpenRouter:MaxReasoningCharsWithoutContent", 12_000),
            2_000,
            100_000);
        string? line;

        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (contentBuilder.Length == 0 && reasoningBuilder.Length == 0)
                {
                    linesWithoutAnyContent++;
                    if (linesWithoutAnyContent >= maxLinesWithoutContent)
                    {
                        throw new HttpRequestException(
                            $"OpenRouter: stream stall — no content for {linesWithoutAnyContent} SSE lines.");
                    }
                }

                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (payload == "[DONE]")
                break;
            if (payload.Length == 0)
                continue;

            JsonElement chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<JsonElement>(payload);
            }
            catch (JsonException)
            {
                continue;
            }

            if (!chunk.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                continue;

            var choice = choices[0];
            if (choice.TryGetProperty("finish_reason", out var finishReason)
                && finishReason.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(finishReason.GetString()))
            {
                break;
            }

            if (!choice.TryGetProperty("delta", out var delta))
                continue;

            var sawContent = false;
            if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
            {
                var piece = contentEl.GetString();
                if (!string.IsNullOrEmpty(piece))
                {
                    contentBuilder.Append(piece);
                    sawContent = true;
                }
            }

            if (delta.TryGetProperty("reasoning", out var reasoningEl) && reasoningEl.ValueKind == JsonValueKind.String)
            {
                var piece = reasoningEl.GetString();
                if (!string.IsNullOrEmpty(piece))
                {
                    reasoningBuilder.Append(piece);
                    sawContent = true;
                }
            }

            if (delta.TryGetProperty("reasoning_content", out var reasoningContentEl)
                && reasoningContentEl.ValueKind == JsonValueKind.String)
            {
                var piece = reasoningContentEl.GetString();
                if (!string.IsNullOrEmpty(piece))
                {
                    reasoningBuilder.Append(piece);
                    sawContent = true;
                }
            }

            if (sawContent)
            {
                linesWithoutAnyContent = 0;
            }
            else if (contentBuilder.Length == 0 && reasoningBuilder.Length == 0)
            {
                linesWithoutAnyContent++;
                if (linesWithoutAnyContent >= maxLinesWithoutContent)
                {
                    throw new HttpRequestException(
                        $"OpenRouter: stream stall — no content for {linesWithoutAnyContent} SSE lines.");
                }
            }

            if (contentBuilder.Length == 0 && reasoningBuilder.Length >= maxReasoningWithoutContent)
            {
                throw new HttpRequestException(
                    $"OpenRouter: reasoning-only stream exceeded {maxReasoningWithoutContent} chars without content.");
            }
        }

        var content = contentBuilder.ToString();
        if (content.Length == 0 && reasoningBuilder.Length > 0)
        {
            _logger.LogWarning(
                "OpenRouter: stream returned no content; using reasoning tail ({ReasoningLen} chars)",
                reasoningBuilder.Length);
            content = reasoningBuilder.ToString();
        }

        if (content.Length == 0)
            throw new HttpRequestException("OpenRouter: stream produced no content.");

        _logger.LogInformation(
            "OpenRouter: Stream complete, content={ContentLen}, reasoning={ReasoningLen}",
            content.Length,
            reasoningBuilder.Length);
        return content;
    }

    private async Task<string> GenerateNonStreamingAsync(string? prompt, string? systemPrompt, string modelName)
    {
        using var request = BuildRequest(prompt, systemPrompt, modelName, stream: false);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        _logger.LogInformation("OpenRouter: Response status {StatusCode}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenRouter: API call failed with status {StatusCode}. Response: {Response}",
                response.StatusCode,
                body);
            throw new HttpRequestException($"OpenRouter API call failed: {response.StatusCode} - {body}");
        }

        var result = JsonSerializer.Deserialize<JsonElement>(body);
        var message = result.GetProperty("choices")[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String
            ? contentEl.GetString() ?? string.Empty
            : string.Empty;

        if (content.Length == 0 && message.TryGetProperty("reasoning", out var reasoningEl)
            && reasoningEl.ValueKind == JsonValueKind.String)
        {
            content = reasoningEl.GetString() ?? string.Empty;
        }

        if (content.Length == 0)
            throw new HttpRequestException("OpenRouter: response contained no message content.");

        _logger.LogInformation("OpenRouter: Successfully generated response with length {Length}", content.Length);
        return content;
    }

    private HttpRequestMessage BuildRequest(string? prompt, string? systemPrompt, string modelName, bool stream)
    {
        var temperature = _configuration.GetValue("AI:OpenRouter:Temperature", 0.3);
        var maxTokens = _configuration.GetValue("AI:OpenRouter:MaxTokens", 8192);
        var disableReasoning = _configuration.GetValue("AI:OpenRouter:DisableReasoning", true);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = modelName,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt ?? "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens,
            ["stream"] = stream
        };

        if (disableReasoning && modelName.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            // DeepSeek V4 on OpenRouter defaults to thinking; disable for codegen/plan JSON.
            payload["reasoning"] = new Dictionary<string, object?> { ["enabled"] = false };
        }

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://libr4.com");
        request.Headers.TryAddWithoutValidation("X-Title", "Libr4");
        if (stream)
            request.Headers.TryAddWithoutValidation("Accept", "text/event-stream");

        return request;
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var modelName = model ?? _configuration["AI:OpenRouter:EmbeddingModel"] ?? "openai/text-embedding-ada-002";

        var requestBody = new
        {
            model = modelName,
            input = text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/embeddings")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
            Version = HttpVersion.Version11
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
        return await GenerateCompletionAsync(prompt, systemPrompt, model).ConfigureAwait(false);
    }

    public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null) =>
        GenerateCompletionAsync(message, systemPrompt, model);

    private static bool IsPrematureStreamEnd(Exception ex) =>
        ex is HttpRequestException { InnerException: HttpIOException io }
        && io.HttpRequestError == HttpRequestError.ResponseEnded;
}
