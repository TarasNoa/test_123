using Grpc.Net.Client;
using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Infrastructure.Crawling;
using Libr4.Matching.Infrastructure.Embeddings;
using Libr4.Matching.Infrastructure.Matching;
using Libr4.Matching.Infrastructure.Persistence;
using Libr4.Matching.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace Libr4.Matching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMatchingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MatchingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "matching")));

        var embeddingsUrl = configuration["Grpc:EmbeddingsUrl"] ?? "http://localhost:50061";
        var crawlerUrl    = configuration["Grpc:CrawlerUrl"]    ?? "http://localhost:50060";
        var qdrantHost    = configuration["Qdrant:Host"]        ?? "localhost";
        var qdrantPort    = int.Parse(configuration["Qdrant:Port"] ?? "6334");

        services.AddSingleton(_ => new QdrantClient(qdrantHost, qdrantPort));

        services.AddSingleton<IVectorIndex>(sp =>
        {
            var client = sp.GetRequiredService<QdrantClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QdrantVectorIndex>>();
            return new QdrantVectorIndex(client, logger);
        });

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var channel = GrpcChannel.ForAddress(embeddingsUrl);
            var logger  = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RustEmbeddingsGrpcClient>>();
            return new RustEmbeddingsGrpcClient(channel, logger);
        });

        services.AddSingleton<ICrawlerService>(sp =>
        {
            var channel = GrpcChannel.ForAddress(crawlerUrl);
            var logger  = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RustCrawlerGrpcClient>>();
            return new RustCrawlerGrpcClient(channel, logger);
        });

        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchingService, HybridMatchingService>();

        return services;
    }
}
