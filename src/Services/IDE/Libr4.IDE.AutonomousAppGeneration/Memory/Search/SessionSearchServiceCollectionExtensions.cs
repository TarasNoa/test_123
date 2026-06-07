using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;

public static class SessionSearchServiceCollectionExtensions
{
    public static IServiceCollection AddSessionSearch(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var useQdrantSync = configuration?.GetValue<bool>("Memory:UseQdrantSync") ?? false;
        if (useQdrantSync)
            services.AddSingleton<ISessionSearchService, HybridSessionSearchService>();
        else
            services.AddSingleton<ISessionSearchService, CompositeSessionSearchService>();

        return services;
    }
}
