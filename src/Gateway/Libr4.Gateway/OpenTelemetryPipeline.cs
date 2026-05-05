using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Libr4.Gateway;

/// <summary>
/// OpenTelemetry pipeline configuration for distributed tracing
/// Traces full path: Next.js → Gateway → IDE → Rust (Obscura)
/// </summary>
public static class OpenTelemetryPipeline
{
    public static IServiceCollection AddOpenTelemetryPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = "Libr4.Gateway";
        var serviceVersion = "1.0.0";
        
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion: serviceVersion)
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "production",
                            ["host.name"] = Environment.MachineName
                        }))
                    
                    // Instrument ASP.NET Core
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = ctx =>
                        {
                            // Filter out health checks
                            var path = ctx.Request.Path.Value ?? "";
                            return !path.StartsWith("/health") && !path.StartsWith("/metrics");
                        };
                    })
                    
                    // Instrument HttpClient (calls to services)
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.request_id", 
                                request.Headers.TryGetValues("X-Request-ID", out var values) 
                                    ? values.FirstOrDefault() 
                                    : null);
                        };
                    })
                    
                    // YARP instrumentation
                    .AddSource("Yarp.ReverseProxy")
                    
                    // Custom sources
                    .AddSource("Libr4.Gateway.CircuitBreaker")
                    .AddSource("Libr4.Gateway.PreviewRouter")
                    
                    // Sampling: trace all requests in dev, 10% in prod
                    .SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(
                            configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ? 1.0 : 0.1)))
                    
                    // Exporters
                    // .AddJaegerExporter(options => // TODO: Uncomment when Jaeger exporter is available
                    // {
                    //     options.AgentHost = configuration["Jaeger:Host"] ?? "localhost";
                    //     options.AgentPort = int.Parse(configuration["Jaeger:Port"] ?? "6831");
                    // })
                    // .AddOtlpExporter(options => // TODO: Uncomment when OTLP exporter is available
                    // {
                    //     options.Endpoint = new Uri(configuration["Otlp:Endpoint"] ?? "http://localhost:4317");
                    // })
                    // .AddConsoleExporter(options => // TODO: Uncomment when ConsoleExporter is available
                    // {
                    //     options.Targets = ConsoleExporterOutputTargets.Console;
                    // });
                    ; // Empty statement - exporters commented out
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion: serviceVersion))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // .AddProcessInstrumentation() // TODO: Uncomment when available
                    .AddPrometheusExporter();
            });

        // Add correlation ID middleware support
        services.AddSingleton<CorrelationContext>();
        
        return services;
    }
}

/// <summary>
/// Correlation context for distributed tracing
/// </summary>
public class CorrelationContext
{
    private readonly AsyncLocal<string?> _traceId = new();
    private readonly AsyncLocal<string?> _spanId = new();
    private readonly AsyncLocal<Dictionary<string, string>> _baggage = new();

    public string? TraceId 
    { 
        get => _traceId.Value; 
        set => _traceId.Value = value;
    }
    
    public string? SpanId 
    { 
        get => _spanId.Value; 
        set => _spanId.Value = value;
    }
    
    public Dictionary<string, string> Baggage 
    { 
        get => _baggage.Value ??= new Dictionary<string, string>();
    }

    /// <summary>
    /// Initialize from incoming request headers
    /// </summary>
    public void InitializeFromHeaders(IHeaderDictionary headers)
    {
        // W3C Trace Context
        if (headers.TryGetValue("traceparent", out var traceParent))
        {
            var parts = traceParent.ToString().Split('-');
            if (parts.Length >= 3)
            {
                TraceId = parts[1];
                SpanId = parts[2];
            }
        }
        
        // Legacy correlation ID
        if (headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            TraceId ??= correlationId.ToString();
        }
        
        // Baggage (W3C)
        if (headers.TryGetValue("tracestate", out var traceState))
        {
            ParseTraceState(traceState.ToString());
        }
    }

    /// <summary>
    /// Propagate to outgoing request
    /// </summary>
    public void PropagateToHeaders(IHeaderDictionary headers)
    {
        if (!string.IsNullOrEmpty(TraceId))
        {
            // W3C Trace Context
            var traceParent = $"00-{TraceId}-{SpanId ?? Activity.Current?.SpanId.ToString() ?? "0000000000000000"}-00";
            headers.Append("traceparent", traceParent);
            
            // Legacy correlation
            headers.Append("X-Correlation-ID", TraceId);
            headers.Append("X-Request-ID", Guid.NewGuid().ToString("N"));
        }
        
        // Propagate baggage
        if (Baggage.Any())
        {
            var traceState = string.Join(",", 
                Baggage.Select(b => $"{Uri.EscapeDataString(b.Key)}={Uri.EscapeDataString(b.Value)}"));
            headers.Append("tracestate", traceState);
        }
    }

    /// <summary>
    /// Propagate to outgoing HttpClient request
    /// </summary>
    public void PropagateToHeaders(System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        if (!string.IsNullOrEmpty(TraceId))
        {
            // W3C Trace Context
            var traceParent = $"00-{TraceId}-{SpanId ?? Activity.Current?.SpanId.ToString() ?? "0000000000000000"}-00";
            headers.TryAddWithoutValidation("traceparent", traceParent);
            
            // Legacy correlation
            headers.TryAddWithoutValidation("X-Correlation-ID", TraceId);
            headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString("N"));
        }
        
        // Propagate baggage
        if (Baggage.Any())
        {
            var traceState = string.Join(",", 
                Baggage.Select(b => $"{Uri.EscapeDataString(b.Key)}={Uri.EscapeDataString(b.Value)}"));
            headers.TryAddWithoutValidation("tracestate", traceState);
        }
    }

    private void ParseTraceState(string traceState)
    {
        var pairs = traceState.Split(',');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0].Trim());
                var value = Uri.UnescapeDataString(parts[1].Trim());
                Baggage[key] = value;
            }
        }
    }
}

/// <summary>
/// Middleware to propagate correlation IDs
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationContext _correlationContext;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        CorrelationContext correlationContext,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Initialize correlation from incoming request
        _correlationContext.InitializeFromHeaders(context.Request.Headers);
        
        // Generate new trace ID if none exists
        if (string.IsNullOrEmpty(_correlationContext.TraceId))
        {
            _correlationContext.TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }
        
        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!string.IsNullOrEmpty(_correlationContext.TraceId))
            {
                context.Response.Headers["X-Correlation-ID"] = _correlationContext.TraceId;
                context.Response.Headers["X-Trace-ID"] = _correlationContext.TraceId;
            }
            return Task.CompletedTask;
        });

        // Add baggage for downstream services
        _correlationContext.Baggage["gateway.timestamp"] = DateTime.UtcNow.ToString("O");
        _correlationContext.Baggage["gateway.request_path"] = context.Request.Path;
        _correlationContext.Baggage["gateway.client_ip"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _correlationContext.TraceId ?? "unknown",
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "unknown",
            ["SpanId"] = Activity.Current?.SpanId.ToString() ?? "unknown"
        }))
        {
            _logger.LogDebug(
                "Request {Method} {Path} with TraceId {TraceId}",
                context.Request.Method,
                context.Request.Path,
                _correlationContext.TraceId);

            await _next(context);

            _logger.LogDebug(
                "Response {StatusCode} for {Path}, duration {DurationMs}ms",
                context.Response.StatusCode,
                context.Request.Path,
                Activity.Current?.Duration.TotalMilliseconds ?? 0);
        }
    }
}

/// <summary>
/// HTTP client handler that propagates correlation IDs
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly CorrelationContext _correlationContext;
    private readonly ILogger<CorrelationIdDelegatingHandler> _logger;

    public CorrelationIdDelegatingHandler(
        CorrelationContext correlationContext,
        ILogger<CorrelationIdDelegatingHandler> logger)
    {
        _correlationContext = correlationContext;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Propagate correlation to outgoing request
        _correlationContext.PropagateToHeaders(request.Headers);
        
        _logger.LogDebug(
            "Outgoing request {Method} {Uri} with TraceId {TraceId}",
            request.Method,
            request.RequestUri,
            _correlationContext.TraceId);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogDebug(
                "Response {StatusCode} from {Uri} in {DurationMs}ms",
                (int)response.StatusCode,
                request.RequestUri,
                stopwatch.ElapsedMilliseconds);

            // Propagate trace info from response
            if (response.Headers.TryGetValues("X-Trace-ID", out var responseTraceIds))
            {
                var traceId = responseTraceIds.FirstOrDefault();
                if (!string.IsNullOrEmpty(traceId))
                {
                    Activity.Current?.SetTag("downstream.trace_id", traceId);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Request failed {Method} {Uri} after {DurationMs}ms",
                request.Method,
                request.RequestUri,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Extensions for OpenTelemetry pipeline
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddCorrelationContext(this IServiceCollection services)
    {
        services.AddSingleton<CorrelationContext>();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        
        // Configure HttpClient with correlation handler
        services.AddHttpClient("Default")
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
        
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
