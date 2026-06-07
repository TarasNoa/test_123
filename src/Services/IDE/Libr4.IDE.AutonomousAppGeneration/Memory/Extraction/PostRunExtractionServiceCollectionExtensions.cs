using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public static class PostRunExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddPostRunExtraction(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<PostRunExtractionOptions>(
                configuration.GetSection("AutonomousAppGeneration:PostRunExtraction"));
        else
            services.Configure<PostRunExtractionOptions>(_ => { });

        services.AddSingleton<HeuristicPostRunExtractor>();
        services.AddScoped<LlmPostRunExtractor>();
        services.AddSingleton<PostRunExtractionRequestBuilder>();
        services.AddSingleton<PostRunLessonIngestor>();
        services.AddScoped<IPostRunExtractor, PostRunExtractor>();
        services.AddSingleton<IPostRunExtractionQueue>(sp =>
            new BoundedPostRunExtractionQueue(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PostRunExtractionOptions>>().Value));
        services.AddHostedService<PostRunExtractionBackgroundService>();
        services.AddSingleton<IAutonomousFinalizationHook, PostRunExtractionFinalizationHook>();
        return services;
    }
}
