using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public static class QdrantSyncServiceCollectionExtensions
{
    public static IServiceCollection AddQdrantSync(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<QdrantSyncOptions>(configuration.GetSection("Memory"));
        else
            services.Configure<QdrantSyncOptions>(_ => { });

        var useQdrantSync = configuration?.GetValue<bool>("Memory:UseQdrantSync") ?? false;
        var embeddingProvider = configuration?["Memory:Embeddings:Provider"]?.Trim().ToLowerInvariant() ?? "ollama";

        if (string.Equals(embeddingProvider, "grpc", StringComparison.Ordinal))
        {
            services.AddSingleton<IEmbeddingService, RustEmbeddingsGrpcClient>();
        }
        else if (string.Equals(embeddingProvider, "ort-cpp", StringComparison.Ordinal))
        {
            services.AddSingleton<IEmbeddingService, CppOrtEmbeddingService>();
        }
        else
        {
            services.AddHttpClient<LocalEmbeddingService>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QdrantSyncOptions>>().Value;
                client.BaseAddress = new Uri(options.Embeddings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();
        }
        services.AddSingleton<IHermesVectorSyncService, HermesVectorSyncService>();

        if (useQdrantSync)
        {
            services.AddHttpClient<QdrantVectorMemoryStore>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QdrantSyncOptions>>().Value;
                client.BaseAddress = new Uri(options.Qdrant.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
                if (!string.IsNullOrWhiteSpace(options.Qdrant.ApiKey))
                    client.DefaultRequestHeaders.Add("api-key", options.Qdrant.ApiKey);
            });
            services.AddSingleton<IVectorMemoryStore, QdrantVectorMemoryStore>();
            services.AddHostedService<HermesVectorBackfillHostedService>();
        }
        else
        {
            services.AddSingleton<IVectorMemoryStore, InProcessVectorMemoryStore>();
        }

        return services;
    }
}
