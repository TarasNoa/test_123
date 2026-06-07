using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public static class AgentStackServiceCollectionExtensions
{
    public static IServiceCollection AddAgentStack(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<AgentStackOptions>(configuration.GetSection(AgentStackOptions.SectionName));
        else
            services.Configure<AgentStackOptions>(_ => { });

        services.AddHttpClient("AgentStackHealth");

        services.AddHttpClient<IShadowSyncClient, ShadowSyncClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentStackOptions>>().Value;
            client.BaseAddress = new Uri(opts.ShadowSyncBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opts.HealthCheckTimeoutSeconds);
        });

        services.AddHttpClient<ISandboxControllerClient, SandboxControllerClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentStackOptions>>().Value;
            client.BaseAddress = new Uri(opts.SandboxControllerBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opts.HealthCheckTimeoutSeconds);
        });

        services.AddSingleton<IAgentStackHealthService, AgentStackHealthService>();
        services.AddSingleton<IAgentStackRunGate, AgentStackRunGate>();
        services.AddHostedService<AgentStackStartupHealthGate>();
        return services;
    }
}
