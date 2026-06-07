using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public static class RunExportServiceCollectionExtensions
{
    public static IServiceCollection AddRunExport(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<RunExportOptions>(configuration.GetSection(RunExportOptions.SectionName));
        else
            services.Configure<RunExportOptions>(_ => { });

        services.AddSingleton<RunSessionSnapshotExporter>();
        services.AddSingleton<IRunExportService, RunExportService>();
        return services;
    }
}
