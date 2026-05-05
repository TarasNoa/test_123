using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Libr4.Shared.Web.Logging;

public static class SerilogExtensions
{
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
