/*
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace Libr4.Gateway;

/// <summary>
/// Circuit breaker middleware for YARP Gateway
/// Protects against cascading failures when Shadow containers fail
/// Shows "Preview Restarting" instead of 502 errors
/// </summary>
public class CircuitBreakerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly ICircuitBreakerState _circuitState;

    public CircuitBreakerMiddleware(
        RequestDelegate next,
        ILogger<CircuitBreakerMiddleware> logger,
        ICircuitBreakerState circuitState)
    {
        _next = next;
        _logger = logger;
        _circuitState = circuitState;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        
        // Only apply circuit breaker to preview routes
        if (!path.StartsWith("/preview/"))
        {
            await _next(context);
            return;
        }

        // Extract order ID from path: /preview/{hash}/{orderId}/...
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3)
        {
            await _next(context);
            return;
        }

        var orderId = segments[2];
        var circuitKey = $"preview-{orderId}";

        // Check if circuit is open for this order
        if (_circuitState.IsOpen(circuitKey))
        {
            _logger.LogWarning(
                "Circuit OPEN for order {OrderId}, returning maintenance page",
                orderId);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html";
            
            await context.Response.WriteAsync(GetMaintenancePage(orderId));
            return;
        }

        try
        {
            // Try to proxy the request
            await _next(context);
            
            // Check if response was successful
            if (context.Response.StatusCode >= 500)
            {
                _circuitState.RecordFailure(circuitKey);
                _logger.LogWarning(
                    "Recording failure for order {OrderId}, status {StatusCode}",
                    orderId, context.Response.StatusCode);
            }
            else
            {
                _circuitState.RecordSuccess(circuitKey);
            }
        }
        catch (Exception ex) when (IsTransientFailure(ex))
        {
            _circuitState.RecordFailure(circuitKey);
            _logger.LogError(ex, 
                "Transient failure for order {OrderId}, circuit breaker triggered",
                orderId);

            // Return maintenance page instead of 502
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html";
            
            await context.Response.WriteAsync(GetMaintenancePage(orderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-transient error for order {OrderId}", orderId);
            throw;
        }
    }

    private static bool IsTransientFailure(Exception ex)
    {
        // Consider these as transient failures:
        // - HttpRequestException (connection issues)
        // - TimeoutException
        // - TaskCanceledException (when it's a timeout)
        
        return ex is HttpRequestException 
            || ex is TimeoutException
            || (ex is TaskCanceledException tce && tce.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetMaintenancePage(string orderId)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <title>Preview Restarting</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .container {{
            text-align: center;
            padding: 40px;
            background: rgba(255,255,255,0.1);
            border-radius: 16px;
            backdrop-filter: blur(10px);
        }}
        .spinner {{
            border: 4px solid rgba(255,255,255,0.3);
            border-top: 4px solid white;
            border-radius: 50%;
            width: 50px;
            height: 50px;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
        h1 {{ margin: 0 0 10px; font-size: 28px; }}
        p {{ margin: 0; opacity: 0.9; }}
        .order-id {{ 
            font-family: monospace; 
            background: rgba(255,255,255,0.2); 
            padding: 4px 8px; 
            border-radius: 4px;
            margin-top: 20px;
            display: inline-block;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='spinner'></div>
        <h1>Preview Restarting</h1>
        <p>The preview is being refreshed. This will take just a moment.</p>
        <div class='order-id'>Order: {orderId[..8]}...</div>
    </div>
    <script>
        // Auto-retry every 5 seconds
        setTimeout(() => window.location.reload(), 5000);
    </script>
</body>
</html>";
    }
}

/// <summary>
/// Circuit breaker state management
/// </summary>
public interface ICircuitBreakerState
{
    bool IsOpen(string key);
    void RecordSuccess(string key);
    void RecordFailure(string key);
    CircuitStats GetStats(string key);
}

/// <summary>
/// Implementation with per-order circuit state
/// </summary>
public class CircuitBreakerState : ICircuitBreakerState, IDisposable
{
    private readonly Dictionary<string, CircuitState> _states = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Timer _cleanupTimer;
    private readonly ILogger<CircuitBreakerState> _logger;
    
    // Configuration
    private const int FailureThreshold = 5;        // Open after 5 failures
    private const int SuccessThreshold = 3;        // Close after 3 successes
    private const int OpenDurationSeconds = 30;    // Stay open for 30 seconds
    private const int SamplingDurationSeconds = 60; // Sample window

    public CircuitBreakerState(ILogger<CircuitBreakerState> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(
            CleanupInactiveCircuits,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    public bool IsOpen(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_states.TryGetValue(key, out var state))
            {
                return false; // No state = closed
            }

            // Check if open duration has elapsed
            if (state.Status == CircuitStatus.Open)
            {
                var openDuration = DateTime.UtcNow - state.LastStateChange;
                if (openDuration.TotalSeconds >= OpenDurationSeconds)
                {
                    // Transition to half-open
                    state.Status = CircuitStatus.HalfOpen;
                    state.LastStateChange = DateTime.UtcNow;
                    state.ConsecutiveSuccesses = 0;
                    state.ConsecutiveFailures = 0;
                    _logger.LogInformation(
                        "Circuit for {Key} transitioning from Open to HalfOpen",
                        key);
                }
            }

            return state.Status == CircuitStatus.Open;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void RecordSuccess(string key)
    {
        _lock.EnterWriteLock();
        try
        {
            var state = GetOrCreateState(key);
            
            state.ConsecutiveSuccesses++;
            state.ConsecutiveFailures = 0;
            state.LastSuccess = DateTime.UtcNow;

            // Transition from HalfOpen to Closed
            if (state.Status == CircuitStatus.HalfOpen && state.ConsecutiveSuccesses >= SuccessThreshold)
            {
                state.Status = CircuitStatus.Closed;
                state.LastStateChange = DateTime.UtcNow;
                state.ConsecutiveSuccesses = 0;
                _logger.LogInformation(
                    "Circuit for {Key} CLOSED after {Successes} consecutive successes",
                    key, SuccessThreshold);
            }

            // Prune old failures
            var cutoff = DateTime.UtcNow.AddSeconds(-SamplingDurationSeconds);
            state.RecentFailures.RemoveAll(f => f < cutoff);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RecordFailure(string key)
    {
        _lock.EnterWriteLock();
        try
        {
            var state = GetOrCreateState(key);
            
            state.ConsecutiveFailures++;
            state.ConsecutiveSuccesses = 0;
            state.RecentFailures.Add(DateTime.UtcNow);
            state.LastFailure = DateTime.UtcNow;

            // Check if should open circuit
            var failuresInWindow = state.RecentFailures
                .Count(f => f > DateTime.UtcNow.AddSeconds(-SamplingDurationSeconds));

            if (state.Status != CircuitStatus.Open && failuresInWindow >= FailureThreshold)
            {
                state.Status = CircuitStatus.Open;
                state.LastStateChange = DateTime.UtcNow;
                _logger.LogWarning(
                    "Circuit for {Key} OPENED after {Failures} failures in {Seconds}s",
                    key, failuresInWindow, SamplingDurationSeconds);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public CircuitStats GetStats(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_states.TryGetValue(key, out var state))
            {
                return new CircuitStats { Status = CircuitStatus.Closed.ToString() };
            }

            return new CircuitStats
            {
                Status = state.Status.ToString(),
                ConsecutiveFailures = state.ConsecutiveFailures,
                ConsecutiveSuccesses = state.ConsecutiveSuccesses,
                RecentFailureCount = state.RecentFailures.Count,
                LastStateChange = state.LastStateChange,
                LastFailure = state.LastFailure,
                LastSuccess = state.LastSuccess
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private CircuitState GetOrCreateState(string key)
    {
        if (!_states.TryGetValue(key, out var state))
        {
            state = new CircuitState
            {
                Key = key,
                Status = CircuitStatus.Closed,
                LastStateChange = DateTime.UtcNow,
                RecentFailures = new List<DateTime>()
            };
            _states[key] = state;
        }
        return state;
    }

    private void CleanupInactiveCircuits(object? state)
    {
        _lock.EnterWriteLock();
        try
        {
            var inactiveThreshold = DateTime.UtcNow.AddHours(1);
            var toRemove = _states
                .Where(s => s.Value.LastStateChange < inactiveThreshold && 
                           s.Value.Status == CircuitStatus.Closed)
                .Select(s => s.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _states.Remove(key);
            }

            if (toRemove.Count > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} inactive circuits, {Remaining} remaining",
                    toRemove.Count, _states.Count);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _lock?.Dispose();
    }
}

/// <summary>
/// Circuit state for a single order
/// </summary>
public class CircuitState
{
    public string Key { get; set; } = string.Empty;
    public CircuitStatus Status { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public List<DateTime> RecentFailures { get; set; } = new();
    public DateTime LastStateChange { get; set; }
    public DateTime? LastFailure { get; set; }
    public DateTime? LastSuccess { get; set; }
}

public enum CircuitStatus
{
    Closed,     // Normal operation
    Open,       // Failing fast
    HalfOpen    // Testing if service recovered
}

public class CircuitStats
{
    public string Status { get; set; } = string.Empty;
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public int RecentFailureCount { get; set; }
    public DateTime LastStateChange { get; set; }
    public DateTime? LastFailure { get; set; }
    public DateTime? LastSuccess { get; set; }
}

/// <summary>
/// Extension method to register circuit breaker
/// </summary>
public static class CircuitBreakerExtensions
{
    public static IServiceCollection AddCircuitBreaker(
        this IServiceCollection services)
    {
        services.AddSingleton<ICircuitBreakerState, CircuitBreakerState>();
        return services;
    }

    public static IApplicationBuilder UseCircuitBreaker(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<CircuitBreakerMiddleware>();
    }
}
*/
