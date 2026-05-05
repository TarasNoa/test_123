using Libr4.IDE.Application.CodeSearch;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// DI registration for the Semantic Code Index subsystem (SocratiCode equivalent).
/// Call from Program.cs or a service DI extension.
/// </summary>
public static class SemanticIndexServiceExtensions
{
    public static IServiceCollection AddSemanticCodeIndex(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<SemanticIndexOptions>(
            configuration.GetSection("SemanticIndex"));
        services.Configure<OllamaEmbeddingOptions>(
            configuration.GetSection("SemanticIndex:Ollama"));
        services.Configure<QdrantOptions>(
            configuration.GetSection("SemanticIndex:Qdrant"));

        // Embedding HTTP client
        services.AddHttpClient<OllamaEmbeddingService>(client =>
        {
            var baseUrl = configuration["SemanticIndex:Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Qdrant HTTP client
        services.AddHttpClient<QdrantVectorMemoryStore>(client =>
        {
            var baseUrl = configuration["SemanticIndex:Qdrant:BaseUrl"] ?? "http://localhost:6333";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
            var apiKey = configuration["SemanticIndex:Qdrant:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("api-key", apiKey);
        });

        // Core services
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IVectorMemoryStore, QdrantVectorMemoryStore>();
        services.AddSingleton<ISemanticCodeIndex, SemanticCodeIndexService>();
        services.AddSingleton<ICodeContextArtifactService, CodeContextArtifactService>();

        return services;
    }
}
