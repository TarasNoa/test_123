using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Libr4.Shared.Web.Logging;

public static class SerilogExtensions
{
    public static IServiceCollection AddLibr4Logging(this IServiceCollection services)
    {
        // Serilog is configured via builder.Host.UseSerilog — this overload is a no-op stub
        // for services that call AddLibr4Logging() on IServiceCollection directly.
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
