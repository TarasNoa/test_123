using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public static class LiveSearchServiceCollectionExtensions
{
    public static IServiceCollection AddLiveSearch(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<LiveSearchOptions>(configuration.GetSection(LiveSearchOptions.SectionName));
        else
            services.Configure<LiveSearchOptions>(_ => { });

        services.AddSingleton<LiveSearchRateLimiter>();
        services.AddSingleton<LiveSearchCache>();

        services.AddHttpClient<DuckDuckGoLiveSearchBackend>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Libr4LiveSearch/1.0");
        });
        services.AddHttpClient<BraveLiveSearchBackend>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Libr4LiveSearch/1.0");
        });
        services.AddHttpClient<XLiveSearchBackend>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Libr4LiveSearch/1.0");
        });

        services.AddSingleton<ILiveSearchService, LiveSearchService>();
        return services;
    }
}
