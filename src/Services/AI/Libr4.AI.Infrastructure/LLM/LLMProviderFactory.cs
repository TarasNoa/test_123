using Microsoft.Extensions.DependencyInjection;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Chats;

namespace Libr4.AI.Infrastructure.LLM;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<AIProviderType, Type> _providers;

    public LLMProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providers = new Dictionary<AIProviderType, Type>
        {
            // Ollama moved to AI.Providers namespace
            [AIProviderType.OpenAI] = typeof(OpenAIProvider),
            // Additional providers can be registered here
        };
    }

    public ILLMProvider GetProvider(AIProviderType type)
    {
        if (_providers.TryGetValue(type, out var providerType))
        {
            return (ILLMProvider)_serviceProvider.GetRequiredService(providerType);
        }

        throw new NotSupportedException($"Provider {type} is not supported");
    }

    public ILLMProvider GetProvider(string model)
    {
        // Simple model-based routing
        return model.StartsWith("llama") || model.StartsWith("mistral") || model.StartsWith("codellama")
            ? GetProvider(AIProviderType.Ollama)
            : GetProvider(AIProviderType.OpenAI);
    }

    public IEnumerable<string> GetAvailableModels()
    {
        return new[]
        {
            "llama2",
            "llama3",
            "mistral",
            "codellama",
            "gpt-4",
            "gpt-4-turbo",
            "gpt-3.5-turbo"
        };
    }
}
