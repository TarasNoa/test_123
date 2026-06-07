using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public static class RunHandoffServiceCollectionExtensions
{
    public static IServiceCollection AddRunHandoff(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddRunExport(configuration);

        if (configuration is not null)
            services.Configure<RunImportOptions>(configuration.GetSection(RunImportOptions.SectionName));
        else
            services.Configure<RunImportOptions>(_ => { });

        services.AddSingleton<IRunImportIdempotencyStore, FileRunImportIdempotencyStore>();
        services.AddSingleton<RunSessionSnapshotImporter>();
        services.AddSingleton<RunEnvironmentUrlRemapper>();
        services.AddSingleton<IRunImportService, RunImportService>();
        services.AddSingleton<IRunPromoteService, RunPromoteService>();
        services.AddHostedService<RunExportRetentionHostedService>();

        var useMassTransit = configuration?.GetValue("AutonomousAppGeneration:AgentScheduling:UseMassTransit", false) == true;
        if (useMassTransit)
            services.AddSingleton<IRunPromoteDispatcher, MassTransitRunPromoteDispatcher>();
        else
            services.AddSingleton<IRunPromoteDispatcher, NoOpRunPromoteDispatcher>();

        if (configuration is not null)
            services.Configure<RunSyncOptions>(configuration.GetSection(RunSyncOptions.SectionName));
        else
            services.Configure<RunSyncOptions>(_ => { });

        services.AddSingleton<IRunSyncCoordinator, RunSyncCoordinator>();
        services.AddSingleton<RunSyncHub>();
        services.AddSingleton<RunSyncBridgeHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<RunSyncBridgeHostedService>());

        return services;
    }

    public static void AddRunHandoffMassTransitConsumers(this IBusRegistrationConfigurator configurator) =>
        configurator.AddConsumer<RunHandoffPromoteConsumer>();
}
