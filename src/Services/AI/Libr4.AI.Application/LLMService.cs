using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace Libr4.AI.Application;

public class LLMService : ILLMService
{
    private readonly ILLMProvider _primaryProvider;
    private readonly ILLMProvider _fallbackProvider;
    private readonly AsyncRetryPolicy _retryPolicy;

    public LLMService(ILLMProviderFactory factory, IConfiguration configuration)
    {
        _primaryProvider = factory.GetProvider(configuration["LLM:DefaultProvider"] ?? "openai");
        _fallbackProvider = factory.GetProvider("ollama");
        _retryPolicy = Policy.Handle<Exception>().RetryAsync(2);
    }

    public async Task<string> GenerateCodeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var enhancedPrompt = $"Generate clean, efficient C# code for: {prompt}. Include comments and error handling.";
        var request = new ChatCompletionRequest("gpt-4", [new ChatMessage("user", enhancedPrompt)]);

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _primaryProvider.CompleteAsync(request, cancellationToken);
                return result.IsSuccess ? result.Value.Content : throw new InvalidOperationException(result.Error?.Message);
            });
        }
        catch
        {
            var fallbackResult = await _fallbackProvider.CompleteAsync(request, cancellationToken);
            return fallbackResult.IsSuccess ? fallbackResult.Value.Content : throw new InvalidOperationException("All LLM providers failed");
        }
    }

    public async Task<string> ExplainCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var prompt = $"Explain this code in simple terms: {code}";
        var request = new ChatCompletionRequest("gpt-4", [new ChatMessage("user", prompt)]);
        var result = await _primaryProvider.CompleteAsync(request, cancellationToken);
        return result.IsSuccess ? result.Value.Content : throw new InvalidOperationException(result.Error?.Message);
    }

    public async Task<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("Embeddings should be retrieved via IEmbeddingProvider, not ILLMService.");
    }
}