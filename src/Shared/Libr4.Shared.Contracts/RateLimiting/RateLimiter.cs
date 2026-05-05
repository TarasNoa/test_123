namespace Libr4.Shared.Contracts.RateLimiting;

/// <summary>
/// Request delegate signature
/// </summary>
public delegate Task RequestDelegate(HttpContext context);

/// <summary>
/// HTTP context (simplified)
/// </summary>
public class HttpContext
{
    public Dictionary<string, string> Items { get; set; } = new();
    public string RequestPath { get; set; } = string.Empty;
    public HttpUser? User { get; set; }
    public HttpConnection? Connection { get; set; }
    public HttpRequest? Request { get; set; }
    public HttpResponse? Response { get; set; }
}

public class HttpUser
{
    public Claim? FindFirst(string claimType)
    {
        return null;
    }
}

public class Claim
{
    public string? Value { get; set; }
}

public class HttpConnection
{
    public string? RemoteIpAddress { get; set; }
    public string? FindFirst(string claimType)
    {
        return null;
    }
}

public class HttpRequest
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Query { get; set; } = new();
}

public class HttpResponse
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    
    public Task WriteAsJsonAsync(object value)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Rate limiting result.
/// </summary>
public record RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// Number of requests remaining in the window.
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    /// When the rate limit window resets (UTC).
    /// </summary>
    public DateTime ResetAt { get; init; }

    /// <summary>
    /// Error message if not allowed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Rate limiting configuration.
/// </summary>
public record RateLimitConfig
{
    /// <summary>
    /// Maximum number of requests allowed in the window.
    /// </summary>
    public int MaxRequests { get; init; } = 10;

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan Window { get; init; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Whether rate limiting is enabled for users with API keys.
    /// </summary>
    public bool LimitApiKeyUsers { get; init; } = false;

    /// <summary>
    /// Custom limit multiplier for API key users (e.g., 10x higher limit).
    /// </summary>
    public int ApiKeyMultiplier { get; init; } = 10;
}

/// <summary>
/// Rate limiter interface.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request is allowed based on rate limits.
    /// </summary>
    /// <param name="identifier">Unique identifier (user ID, IP address, etc.).</param>
    /// <param name="config">Rate limit configuration.</param>
    /// <param name="hasApiKey">Whether the user has an API key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rate limit result.</returns>
    Task<RateLimitResult> CheckRateLimitAsync(
        string identifier,
        RateLimitConfig config,
        bool hasApiKey = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for rate limiting purposes.
    /// </summary>
    /// <param name="identifier">Unique identifier.</param>
    /// <param name="config">Rate limit configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordRequestAsync(
        string identifier,
        RateLimitConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for an identifier.
    /// </summary>
    /// <param name="identifier">Unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetRateLimitAsync(
        string identifier,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory rate limiter for development and testing.
/// </summary>
public class InMemoryRateLimiter : IRateLimiter
{
    private readonly Dictionary<string, RateLimitState> _states = new();
    private readonly object _lock = new();

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string identifier,
        RateLimitConfig config,
        bool hasApiKey = false,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            var effectiveMaxRequests = hasApiKey && config.LimitApiKeyUsers
                ? config.MaxRequests * config.ApiKeyMultiplier
                : config.MaxRequests;

            if (!_states.TryGetValue(identifier, out var state))
            {
                state = new RateLimitState
                {
                    Count = 0,
                    WindowStart = DateTime.UtcNow
                };
                _states[identifier] = state;
            }

            // Check if window has expired
            if (DateTime.UtcNow > state.WindowStart + config.Window)
            {
                state.Count = 0;
                state.WindowStart = DateTime.UtcNow;
            }

            // Check if limit exceeded
            if (state.Count >= effectiveMaxRequests)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Remaining = 0,
                    ResetAt = state.WindowStart + config.Window,
                    Error = $"Rate limit exceeded. Maximum {effectiveMaxRequests} requests per {config.Window}."
                };
            }

            return new RateLimitResult
            {
                IsAllowed = true,
                Remaining = effectiveMaxRequests - state.Count,
                ResetAt = state.WindowStart + config.Window
            };
        }
    }

    public async Task RecordRequestAsync(
        string identifier,
        RateLimitConfig config,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (!_states.TryGetValue(identifier, out var state))
            {
                state = new RateLimitState
                {
                    Count = 0,
                    WindowStart = DateTime.UtcNow
                };
                _states[identifier] = state;
            }

            // Check if window has expired
            if (DateTime.UtcNow > state.WindowStart + config.Window)
            {
                state.Count = 0;
                state.WindowStart = DateTime.UtcNow;
            }

            state.Count++;
        }
    }

    public async Task ResetRateLimitAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_states.TryGetValue(identifier, out var state))
            {
                state.Count = 0;
                state.WindowStart = DateTime.UtcNow;
            }
        }
    }
}

/// <summary>
/// Internal rate limit state.
/// </summary>
internal class RateLimitState
{
    public int Count { get; set; }
    public DateTime WindowStart { get; set; }
}

/// <summary>
/// Rate limiting middleware for ASP.NET Core.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitConfig _config;

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        RateLimitConfig config)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get identifier (user ID or IP address)
        var identifier = GetIdentifier(context);
        var hasApiKey = HasApiKey(context);

        // Check rate limit
        var result = await _rateLimiter.CheckRateLimitAsync(identifier, _config, hasApiKey);

        if (!result.IsAllowed)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsJsonAsync(new
            {
                error = result.Error,
                resetAt = result.ResetAt
            });
            return;
        }

        // Record the request
        await _rateLimiter.RecordRequestAsync(identifier, _config);

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = _config.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = result.ResetAt.ToString("O");

        await _next(context);
    }

    private string GetIdentifier(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{ipAddress ?? "unknown"}";
    }

    private bool HasApiKey(HttpContext context)
    {
        // Check for API key in header or query parameter
        var apiKey = context.Request.Headers.TryGetValue("X-API-Key", out var headerValue) ? headerValue : null;
        if (!string.IsNullOrEmpty(apiKey))
            return true;

        apiKey = context.Request.Query.TryGetValue("apiKey", out var queryValue) ? queryValue : null;
        return !string.IsNullOrEmpty(apiKey);
    }
}
