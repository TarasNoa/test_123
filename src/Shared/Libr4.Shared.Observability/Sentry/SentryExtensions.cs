using Sentry;
using Sentry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Shared.Observability.Sentry;

public static class SentryExtensions
{
    public static IServiceCollection AddLibr4Sentry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var dsn = configuration["Sentry:Dsn"];
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "development";
        
        if (!string.IsNullOrEmpty(dsn))
        {
            services.AddSentry(options =>
            {
                options.Dsn = dsn;
                options.Environment = environment;
                options.Release = configuration["Sentry:Release"] ?? "unknown";
                options.ServerName = serviceName;
                options.TracesSampleRate = double.TryParse(configuration["Sentry:TracesSampleRate"], out var rate) ? rate : 0.1;
                options.ProfilesSampleRate = double.TryParse(configuration["Sentry:ProfilesSampleRate"], out var profileRate) ? profileRate : 0.01;
                
                // Enable performance monitoring
                options.EnableTracing = true;
                
                // Add breadcrumbs for HTTP requests
                options.MaxBreadcrumbs = 100;
                options.MaxQueueItems = 100;
                
                // Filter sensitive data
                options.SetBeforeSend((@event, hint) =>
                {
                    // Remove sensitive headers
                    if (@event.Request?.Headers != null)
                    {
                        @event.Request.Headers.Remove("Authorization");
                        @event.Request.Headers.Remove("Cookie");
                        @event.Request.Headers.Remove("X-Api-Key");
                    }
                    
                    // Remove sensitive form data
                    if (@event.Request?.Data != null)
                    {
                        var data = @event.Request.Data.ToString();
                        if (data != null)
                        {
                            // Mask passwords in request body
                            @event.Request.Data = System.Text.RegularExpressions.Regex.Replace(
                                data, 
                                @"""password""""":\s*""""""[^""""""]*""""""", 
                                @""""""password"""""":""""""*****""""""");
                        }
                    }
                    
                    return @event;
                });
                
                // Tag with service info
                options.SetTag("service", serviceName);
                options.SetTag("version", configuration["Sentry:Release"] ?? "unknown");
            });
            
            services.AddSentryTracing();
        }
        
        return services;
    }
    
    public static IApplicationBuilder UseLibr4Sentry(this IApplicationBuilder app)
    {
        app.UseSentryTracing();
        return app;
    }
}
