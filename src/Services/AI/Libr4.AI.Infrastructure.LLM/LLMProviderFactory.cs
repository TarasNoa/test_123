using Microsoft.Extensions.Configuration;

namespace Libr4.AI.Infrastructure.LLM;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IConfiguration _configuration;

    public LLMProviderFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ILLMProvider GetProvider(string providerName)
    {
        return providerName.ToLower() switch
        {
            "openai" => new OpenAiProvider(_configuration["OpenAI:ApiKey"]),
            "ollama" => new OllamaProvider(_configuration["Ollama:BaseUrl"]),
            _ => throw new ArgumentException($"Unknown provider: {providerName}")
        };
    }
}