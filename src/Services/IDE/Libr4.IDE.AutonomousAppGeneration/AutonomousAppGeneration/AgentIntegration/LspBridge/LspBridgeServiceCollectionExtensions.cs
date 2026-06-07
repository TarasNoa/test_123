using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public static class LspBridgeServiceCollectionExtensions
{
    public static IServiceCollection AddLspBridge(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<LspBridgeOptions>(configuration.GetSection(LspBridgeOptions.SectionName));
        else
            services.Configure<LspBridgeOptions>(_ => { });

        services.AddSingleton<ProcessLspClient>();
        services.AddSingleton<ILspBridge, LspBridge>();
        return services;
    }
}
