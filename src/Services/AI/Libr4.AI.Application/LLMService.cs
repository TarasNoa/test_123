using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.LLM;
using Polly;

namespace Libr4.AI.Application;

public class LLMService : ILLMService
{
    private readonly ILLMProvider _primaryProvider;
    private readonly ILLMProvider _fallbackProvider;

    public LLMService(ILLMProviderFactory factory, IConfiguration configuration)
    {
        _primaryProvider = factory.GetProvider(configuration["LLM:DefaultProvider"] ?? "openai");
        _fallbackProvider = factory.GetProvider("ollama");
    }

    public async Task<string> GenerateCodeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var policy = Policy<string>
            .Handle<Exception>()
            .FallbackAsync(async ct => await _fallbackProvider.GenerateTextAsync(prompt, ct))
            .WrapAsync(Policy.Handle<Exception>().RetryAsync(2));

        var enhancedPrompt = $"Generate clean, efficient C# code for: {prompt}. Include comments and error handling.";
        return await policy.ExecuteAsync(async () => await _primaryProvider.GenerateTextAsync(enhancedPrompt, cancellationToken));
    }

    public async Task<string> ExplainCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var prompt = $"Explain this code in simple terms: {code}";
        return await _provider.GenerateTextAsync(prompt, cancellationToken);
    }

    public async Task<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _provider.GenerateEmbeddingsAsync(text, cancellationToken);
    }
}