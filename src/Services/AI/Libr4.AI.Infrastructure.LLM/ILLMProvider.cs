using System.Threading.Tasks;

namespace Libr4.AI.Infrastructure.LLM;

public interface ILLMProvider
{
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
}

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(string providerName);
}