using Grpc.Net.Client;
using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Infrastructure.Clients;
using Libr4.Matching.Infrastructure.Crawling;
using Libr4.Matching.Infrastructure.Embeddings;
using Libr4.Matching.Infrastructure.Matching;
using Libr4.Matching.Infrastructure.Persistence;
using Libr4.Matching.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var qdrantPort    = int.Parse(configuration["Qdrant:Port"] ?? "6333");

        services.AddSingleton<IVectorIndex>(sp =>
        {
            var http = new HttpClient { BaseAddress = new Uri($"http://{qdrantHost}:{qdrantPort}") };
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QdrantRestVectorIndex>>();
            return new QdrantRestVectorIndex(http, logger);
        });

        // Use simple local embedding service for E2E/testing (no external gRPC dependency)
        services.AddSingleton<IEmbeddingService, SimpleEmbeddingService>();

        services.AddSingleton<ICrawlerService>(sp =>
        {
            var channel = GrpcChannel.ForAddress(crawlerUrl, new GrpcChannelOptions
            {
                HttpHandler = new System.Net.Http.SocketsHttpHandler { EnableMultipleHttp2Connections = true },
            });
            var logger  = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RustCrawlerGrpcClient>>();
            return new RustCrawlerGrpcClient(channel, logger);
        });

        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchingService, HybridMatchingService>();
        services.AddHostedService<EnsureCollectionsHostedService>();

        // HTTP client to fetch task data from Tasks API
        var tasksApiUrl = configuration["Services:TasksApiUrl"] ?? "http://localhost:5012";
        services.AddHttpClient<ITaskDataClient, HttpTaskDataClient>(client =>
        {
            client.BaseAddress = new Uri(tasksApiUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
