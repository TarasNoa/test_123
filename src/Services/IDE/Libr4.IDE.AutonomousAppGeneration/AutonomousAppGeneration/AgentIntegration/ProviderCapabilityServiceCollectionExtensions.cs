using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public static class ProviderCapabilityServiceCollectionExtensions
{
    public static IServiceCollection AddProviderCapabilityMatrix(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<ProviderMatrixOptions>(
                configuration.GetSection(ProviderMatrixOptions.SectionName));
            services.Configure<BudgetOptions>(
                configuration.GetSection(BudgetOptions.SectionName));
        }
        else
        {
            services.Configure<ProviderMatrixOptions>(_ => { });
            services.Configure<BudgetOptions>(_ => { });
        }

        services.AddSingleton<IBudgetService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BudgetOptions>>().Value;
            return new InMemoryBudgetService(options);
        });
        services.AddSingleton<IRunProviderCostTracker, RunProviderCostTracker>();
        services.AddSingleton<IProviderCapabilityMatrix, DefaultProviderCapabilityMatrix>();
        return services;
    }
}
