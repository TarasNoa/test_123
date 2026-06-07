using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public static class HonchoMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddHonchoMemory(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<HonchoMemoryOptions>(configuration.GetSection(HonchoMemoryOptions.SectionName));
        else
            services.Configure<HonchoMemoryOptions>(_ => { });

        services.AddSingleton<IPersonaStore, FilePersonaStore>();
        services.AddSingleton<IHonchoMemoryService, HonchoMemoryService>();
        services.AddSingleton<IAutonomousFinalizationHook, HonchoMemoryFinalizationHook>();
        services.AddSingleton<NullHonchoMemoryClient>();

        services.AddHttpClient<HonchoHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HonchoMemoryOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        services.RemoveAll<IHonchoMemoryClient>();
        services.AddSingleton<IHonchoMemoryClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<HonchoMemoryOptions>>().Value;
            if (options.Enabled && options.UseRemoteDialectic && options.HasRemoteCredentials)
                return sp.GetRequiredService<HonchoHttpClient>();
            return sp.GetRequiredService<NullHonchoMemoryClient>();
        });

        return services;
    }
}
