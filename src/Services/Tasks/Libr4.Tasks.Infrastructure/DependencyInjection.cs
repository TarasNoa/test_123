using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Infrastructure.Persistence;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Kernel.Time;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Tasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTasksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres missing");

        services.AddDbContext<TasksDbContext>(o =>
            o.UseNpgsql(cs, npg => npg.MigrationsAssembly(typeof(TasksDbContext).Assembly.GetName().Name)));
        services.AddScoped<ITasksDbContext>(sp => sp.GetRequiredService<TasksDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddLibr4Redis(configuration);
        services.AddLibr4MassTransit(configuration, x =>
        {
            x.AddConsumers(typeof(DependencyInjection).Assembly);
        });

        services.AddHealthChecks()
            .AddNpgSql(cs, name: "postgres", tags: new[] { "db" });

        return services;
    }
}
