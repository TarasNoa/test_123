using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI;

public class AIProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIProviderFactory> _logger;

    public AIProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AIProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public IAIProvider GetProvider(string? region = null)
    {
        // Determine provider based on configuration and region
        var providerName = GetProviderForRegion(region);
        
        _logger.LogInformation("Using AI provider: {Provider} for region: {Region}", providerName, region ?? "default");

        return providerName switch
        {
            "OpenRouter" => _serviceProvider.GetRequiredService<Providers.OpenRouterProvider>(),
            "AlibabaCloud" => _serviceProvider.GetRequiredService<Providers.AlibabaCloudProvider>(),
            "DockerModelRunner" => _serviceProvider.GetRequiredService<Providers.DockerModelRunnerProvider>(),
            "Ollama" => _serviceProvider.GetRequiredService<Providers.OllamaProvider>(),
            "OpenAI" => _serviceProvider.GetRequiredService<Providers.OpenAIProvider>(),
            "Claude" => _serviceProvider.GetRequiredService<Providers.ClaudeProvider>(),
            "Google" => _serviceProvider.GetRequiredService<Providers.GoogleProvider>(),
            "DeepSeek" => _serviceProvider.GetRequiredService<Providers.DeepSeekProvider>(),
            "GLM" => _serviceProvider.GetRequiredService<Providers.GLMProvider>(),
            _ => throw new InvalidOperationException($"Unknown AI provider: {providerName}")
        };
    }

    private string GetProviderForRegion(string? region)
    {
        // Check region-specific configuration
        if (!string.IsNullOrEmpty(region))
        {
            var regionProvider = _configuration[$"AI:Regions:{region}:Provider"];
            if (!string.IsNullOrEmpty(regionProvider))
            {
                return regionProvider;
            }
        }

        // Fall back to default provider
        return _configuration["AI:DefaultProvider"] ?? "OpenRouter";
    }
}
