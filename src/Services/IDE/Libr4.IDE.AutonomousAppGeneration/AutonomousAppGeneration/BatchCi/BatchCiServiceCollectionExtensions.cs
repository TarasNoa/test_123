using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;

public static class BatchCiServiceCollectionExtensions
{
    public static IServiceCollection AddBatchCi(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<AutonomousBatchLlmProfileOptions>(
                configuration.GetSection(AutonomousBatchLlmProfileOptions.SectionName));
            services.Configure<AutonomousBatchCiOptions>(
                configuration.GetSection(AutonomousBatchCiOptions.SectionName));
        }
        else
        {
            services.Configure<AutonomousBatchLlmProfileOptions>(_ => { });
            services.Configure<AutonomousBatchCiOptions>(_ => { });
        }

        services.AddSingleton<IAutonomousBatchLlmProfileScope, AutonomousBatchLlmProfileScope>();
        services.AddSingleton<IBenchmarkRegressionHarness, BenchmarkRegressionHarness>();
        return services;
    }
}
