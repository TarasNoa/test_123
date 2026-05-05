using System.Net.Http.Json;
using Libr4.AI.Api.Models;

namespace Libr4.AI.Api.Providers;

public sealed class OllamaChatProvider : IChatProvider
{
    public string Name => "ollama";
    private readonly HttpClient _http;

    public OllamaChatProvider(HttpClient http) => _http = http;

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken ct)
    {
        var body = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            stream = false,
            options = request.Temperature is { } t ? new { temperature = t } : null
        };

        var resp = await _http.PostAsJsonAsync("/api/chat", body, ct);
        resp.EnsureSuccessStatusCode();

        var doc = await resp.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty Ollama response");

        var msg = new ChatMessage(doc.Message.Role, doc.Message.Content);
        return new ChatCompletionResponse(
            Id: Guid.NewGuid().ToString("N"),
            Model: doc.Model,
            Provider: Name,
            Choices: new[] { msg },
            PromptTokens: doc.PromptEvalCount,
            CompletionTokens: doc.EvalCount);
    }

    private sealed record OllamaResponse(string Model, OllamaMessage Message, int? PromptEvalCount, int? EvalCount);
    private sealed record OllamaMessage(string Role, string Content);
}
