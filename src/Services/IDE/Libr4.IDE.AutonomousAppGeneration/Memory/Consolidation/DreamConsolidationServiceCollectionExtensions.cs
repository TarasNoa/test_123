using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public static class DreamConsolidationServiceCollectionExtensions
{
    public static IServiceCollection AddDreamConsolidation(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<DreamConsolidationOptions>(
                configuration.GetSection("AutonomousAppGeneration:DreamConsolidation"));
        else
            services.Configure<DreamConsolidationOptions>(_ => { });

        services.AddSingleton<IDreamConsolidationService, HermesDreamConsolidationService>();
        services.AddHostedService<DreamConsolidationNightlyHostedService>();
        return services;
    }
}
