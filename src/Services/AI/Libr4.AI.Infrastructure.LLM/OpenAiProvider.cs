using OpenAI;
using OpenAI.Embeddings;
using OpenAI.Chat;

namespace Libr4.AI.Infrastructure.LLM;

public class OpenAiProvider : ILLMProvider
{
    private readonly OpenAIClient _client;

    public OpenAiProvider(string apiKey)
    {
        _client = new OpenAIClient(apiKey);
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient("gpt-4o-mini");
        var response = await chatClient.CompleteChatAsync(new[] { new UserChatMessage(prompt) }, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddingClient = _client.GetEmbeddingClient("text-embedding-3-small");
        var response = await embeddingClient.GenerateEmbeddingsAsync(new[] { text }, cancellationToken: cancellationToken);
        return response.Value[0].ToFloats().ToArray();
    }
}