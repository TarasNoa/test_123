using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public static class FastContextServiceCollectionExtensions
{
    public static IServiceCollection AddFastContext(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<FastContextOptions>(configuration.GetSection(FastContextOptions.SectionName));
        else
            services.Configure<FastContextOptions>(_ => { });

        services.AddSingleton<RipgrepCodeIndex>();
        services.AddSingleton<EmbeddingCodeIndex>();
        services.AddSingleton<RepoGraphRanker>();
        services.AddSingleton<FastContextFusionRanker>();
        services.AddSingleton<ICodebaseIndex, CodebaseIndexService>();
        services.AddSingleton<IFastContextPrefetcher, FastContextPrefetcher>();
        services.AddHostedService<FastContextWorkspaceSyncBridge>();
        return services;
    }
}
