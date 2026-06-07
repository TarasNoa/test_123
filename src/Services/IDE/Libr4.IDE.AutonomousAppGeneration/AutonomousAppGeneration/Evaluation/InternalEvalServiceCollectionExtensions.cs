using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public static class InternalEvalServiceCollectionExtensions
{
    public static IServiceCollection AddInternalEvalHarness(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<InternalEvalOptions>(configuration.GetSection(InternalEvalOptions.SectionName));
        else
            services.Configure<InternalEvalOptions>(_ => { });

        services.AddSingleton<EvalBenchmarkCatalog>();
        services.AddSingleton<IInternalEvalHarness, InternalEvalHarness>();
        return services;
    }
}
