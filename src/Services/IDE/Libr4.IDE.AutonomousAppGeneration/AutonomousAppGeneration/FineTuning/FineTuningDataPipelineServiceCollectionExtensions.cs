using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public static class FineTuningDataPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddFineTuningDataPipeline(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<FineTuningDataPipelineOptions>(
                configuration.GetSection(FineTuningDataPipelineOptions.SectionName));
        else
            services.Configure<FineTuningDataPipelineOptions>(_ => { });

        services.AddSingleton<FineTuningQualityFilter>();
        services.AddSingleton<FineTuningDatasetWriter>();
        services.AddSingleton<IFineTuningDataPipelineService, FineTuningDataPipelineService>();
        services.AddSingleton<IAutonomousFinalizationHook, FineTuningFinalizationHook>();

        return services;
    }
}
