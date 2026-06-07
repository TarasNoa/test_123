using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

public static class AgentModelRoutingServiceCollectionExtensions
{
    public static IServiceCollection AddAgentModelRouting(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<AgentModelRoutingOptions>(configuration.GetSection(AgentModelRoutingOptions.SectionName));
        else
            services.Configure<AgentModelRoutingOptions>(_ => { });

        services.AddSingleton<RoleModelCircuitBreaker>();
        services.AddSingleton<IAgentModelRouter, AgentModelRouter>();
        return services;
    }
}
