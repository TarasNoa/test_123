using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public sealed class WorkspaceTrustSchemaMigrator : IHostedService
{
    private readonly IWorkspaceTrustStore _store;

    public WorkspaceTrustSchemaMigrator(IWorkspaceTrustStore store) => _store = store;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _store.EnsureSchemaAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class WorkspaceTrustServiceCollectionExtensions
{
    public static IServiceCollection AddWorkspaceTrust(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<WorkspaceTrustOptions>(configuration.GetSection(WorkspaceTrustOptions.SectionName));
        else
            services.Configure<WorkspaceTrustOptions>(_ => { });

        services.AddSingleton<IWorkspaceTrustStore, SqliteWorkspaceTrustStore>();
        services.AddSingleton<IWorkspaceTrustService, WorkspaceTrustService>();
        services.AddSingleton<IWorkspaceTrustRunGate, WorkspaceTrustRunGate>();
        services.AddHostedService<WorkspaceTrustSchemaMigrator>();
        return services;
    }
}
