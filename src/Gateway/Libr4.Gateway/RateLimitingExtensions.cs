/*
using System.Threading.RateLimiting;

namespace Libr4.Gateway;

/// <summary>
/// Rate limiting configuration for Gateway preview routes
/// Prevents customers from DDoS-ing preview endpoints
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddPreviewRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Fixed window for preview routes - strict limits
            options.AddFixedWindowLimiter("PreviewStrict", opt =>
            {
                opt.PermitLimit = 30;           // 30 requests
                opt.Window = TimeSpan.FromMinutes(1);  // per minute
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;             // Small queue
            });

            // Sliding window for API calls - moderate limits
            options.AddSlidingWindowLimiter("ApiModerate", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 4;
                opt.QueueLimit = 10;
            });

            // Token bucket for websocket connections - burst allowance
            options.AddTokenBucketLimiter("WebSocketBurst", opt =>
            {
                opt.TokenLimit = 10;            // Initial burst
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 3;
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
                opt.TokensPerPeriod = 5;      // Replenish 5 tokens every 30s
                opt.AutoReplenishment = true;
            });

            // Global rate limiter - very strict for anonymous
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientId = context.User?.Identity?.Name
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,     // 300 requests per 10 minutes
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 20
                    });
            });

            // Rejection response
            options.OnRejected = (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.Append("Retry-After", "60");
                context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please slow down.", token);
                return new ValueTask();
            };
        });

        return services;
    }

    /// <summary>
    /// Apply rate limiting to preview endpoints
    /// </summary>
    public static IEndpointConventionBuilder WithPreviewRateLimit(this IEndpointConventionBuilder builder)
    {
        return builder.RequireRateLimiting("PreviewStrict");
    }

    /// <summary>
    /// Apply rate limiting to websocket endpoints
    /// </summary>
    public static IEndpointConventionBuilder WithWebSocketRateLimit(this IEndpointConventionBuilder builder)
    {
        return builder.RequireRateLimiting("WebSocketBurst");
    }
}
*/
