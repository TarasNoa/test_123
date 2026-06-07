using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public static class ComputerServiceCollectionExtensions
{
    public static IServiceCollection AddComputerSubagent(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<ComputerSubagentOptions>(configuration.GetSection(ComputerSubagentOptions.SectionName));
        else
            services.Configure<ComputerSubagentOptions>(_ => { });

        services.AddScoped<IComputerFlowRunner, ComputerFlowRunner>();
        services.AddScoped<IComputerSubagentService, ComputerSubagentService>();
        return services;
    }
}
