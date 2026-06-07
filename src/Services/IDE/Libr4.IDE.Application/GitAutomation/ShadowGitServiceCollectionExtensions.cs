using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.GitAutomation;

public static class ShadowGitServiceCollectionExtensions
{
    public static IServiceCollection AddShadowGitCheckpoint(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<ShadowGitCheckpointOptions>(configuration.GetSection(ShadowGitCheckpointOptions.SectionName));
        else
            services.Configure<ShadowGitCheckpointOptions>(_ => { });

        services.AddSingleton<IShadowGitCheckpointService, ShadowGitCheckpointService>();
        return services;
    }
}
