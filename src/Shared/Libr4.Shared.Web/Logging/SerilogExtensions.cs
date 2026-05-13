using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Libr4.Shared.Web.Logging;

public static class SerilogExtensions
{
    public static IServiceCollection AddLibr4Logging(this IServiceCollection services)
    {
        // Ensure default logging providers are available.
        // Console/Debug providers are typically registered by the host;
        // this call ensures the logging pipeline is configured when
        // services only have access to IServiceCollection.
        services.AddLogging();
        return services;
    }

    public static WebApplicationBuilder AddLibr4Serilog(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((ctx, sp, cfg) => cfg
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

        return builder;
    }
}
