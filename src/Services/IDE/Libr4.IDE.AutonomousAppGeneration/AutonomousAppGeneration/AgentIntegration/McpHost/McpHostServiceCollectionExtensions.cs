using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public static class McpHostServiceCollectionExtensions
{
    public static IServiceCollection AddMcpHost(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var bound = new McpHostOptions();
        if (configuration is not null)
            configuration.GetSection(McpHostOptions.SectionName).Bind(bound);

        services.Configure<McpHostOptions>(options =>
        {
            if (configuration is not null)
                configuration.GetSection(McpHostOptions.SectionName).Bind(options);
        });

        foreach (var (key, profile) in bound.SseServers)
        {
            if (string.IsNullOrWhiteSpace(profile.BaseUrl))
                continue;

            var baseUrl = profile.BaseUrl.TrimEnd('/') + "/";
            var apiKeyHeader = profile.ApiKeyHeader;
            var apiKey = profile.ApiKey;
            services.AddHttpClient($"McpSse:{key}", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
                if (!string.IsNullOrWhiteSpace(apiKeyHeader) && !string.IsNullOrWhiteSpace(apiKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation(apiKeyHeader, apiKey);
            });
        }

        services.AddSingleton<IMcpHostCatalog, McpHostCatalog>();
        services.AddSingleton<IMcpExternalServerDiscovery, McpExternalServerDiscovery>();
        services.AddSingleton<McpRunHostManager>();
        services.AddSingleton<IMcpRunHostManager>(sp => sp.GetRequiredService<McpRunHostManager>());
        services.AddHostedService<McpRunHostJanitor>();
        return services;
    }
}
