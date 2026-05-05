using System.Net.Http.Headers;
using System.Net.Http.Json;
using Libr4.AI.Api.Models;

namespace Libr4.AI.Api.Providers;

public sealed class OpenAiChatProvider : IChatProvider
{
    public string Name => "openai";
    private readonly HttpClient _http;

    public OpenAiChatProvider(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        var key = cfg["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken ct)
    {
        var body = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = false,
        };

        var resp = await _http.PostAsJsonAsync("/v1/chat/completions", body, ct);
        resp.EnsureSuccessStatusCode();

        var doc = await resp.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty OpenAI response");

        var choices = doc.Choices.Select(c => new ChatMessage(c.Message.Role, c.Message.Content)).ToList();
        return new ChatCompletionResponse(
            Id: doc.Id,
            Model: doc.Model,
            Provider: Name,
            Choices: choices,
            PromptTokens: doc.Usage?.PromptTokens,
            CompletionTokens: doc.Usage?.CompletionTokens);
    }

    private sealed record OpenAiResponse(string Id, string Model, IReadOnlyList<OpenAiChoice> Choices, OpenAiUsage? Usage);
    private sealed record OpenAiChoice(int Index, OpenAiMessage Message);
    private sealed record OpenAiMessage(string Role, string Content);
    private sealed record OpenAiUsage(
        [property: System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")] int? PromptTokens,
        [property: System.Text.Json.Serialization.JsonPropertyName("completion_tokens")] int? CompletionTokens);
}
