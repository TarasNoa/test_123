using System.Net.Http.Json;
using System.Text.Json;

namespace Libr4.AI.Infrastructure.LLM;

public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaProvider(string baseUrl = "http://localhost:11434")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = "llama3.2",
            prompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.GetProperty("response").GetString() ?? string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = "nomic-embed-text",
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }
}