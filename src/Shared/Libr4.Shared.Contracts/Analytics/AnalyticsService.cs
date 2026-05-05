using Libr4.Shared.Contracts.RateLimiting;

namespace Libr4.Shared.Contracts.Analytics;

/// <summary>
/// Analytics event.
/// </summary>
public record AnalyticsEvent
{
    /// <summary>
    /// Event name (e.g., "fragment_generated", "sandbox_created").
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// User ID (if available).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Team ID (if available).
    /// </summary>
    public string? TeamId { get; init; }

    /// <summary>
    /// Event properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Session ID for grouping events.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// IP address (for geolocation).
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; init; }
}

/// <summary>
/// Analytics service interface.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Tracks an analytics event.
    /// </summary>
    /// <param name="eventName">Event name.</param>
    /// <param name="properties">Event properties.</param>
    /// <param name="userId">User ID (optional).</param>
    /// <param name="teamId">Team ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackEventAsync(
        string eventName,
        Dictionary<string, object>? properties = null,
        string? userId = null,
        string? teamId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a page view.
    /// </summary>
    /// <param name="pageName">Page name.</param>
    /// <param name="properties">Page properties.</param>
    /// <param name="userId">User ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackPageViewAsync(
        string pageName,
        Dictionary<string, object>? properties = null,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks user identification.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="properties">User properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IdentifyUserAsync(
        string userId,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending analytics events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory analytics service for development and testing.
/// </summary>
public class InMemoryAnalyticsService : IAnalyticsService
{
    private readonly List<AnalyticsEvent> _events = new();
    private readonly object _lock = new();

    public async Task TrackEventAsync(
        string eventName,
        Dictionary<string, object>? properties = null,
        string? userId = null,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var evt = new AnalyticsEvent
        {
            EventName = eventName,
            UserId = userId,
            TeamId = teamId,
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };

        lock (_lock)
        {
            _events.Add(evt);
        }

        Console.WriteLine($"[Analytics] Event: {eventName}, User: {userId}, Team: {teamId}");
    }

    public async Task TrackPageViewAsync(
        string pageName,
        Dictionary<string, object>? properties = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var props = properties ?? new Dictionary<string, object>();
        props["page"] = pageName;

        await TrackEventAsync("page_view", props, userId, null, cancellationToken);
    }

    public async Task IdentifyUserAsync(
        string userId,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var props = properties ?? new Dictionary<string, object>();
        props["user_id"] = userId;

        await TrackEventAsync("identify", props, userId, null, cancellationToken);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            Console.WriteLine($"[Analytics] Flushed {_events.Count} events");
            _events.Clear();
        }
    }

    /// <summary>
    /// Gets all tracked events (for testing).
    /// </summary>
    public IReadOnlyList<AnalyticsEvent> GetEvents()
    {
        lock (_lock)
        {
            return _events.ToList().AsReadOnly();
        }
    }
}

/// <summary>
/// Pre-defined analytics event names.
/// </summary>
public static class AnalyticsEvents
{
    // Fragment events
    public const string FragmentGenerated = "fragment_generated";
    public const string FragmentEdited = "fragment_edited";
    public const string FragmentExecuted = "fragment_executed";
    public const string FragmentError = "fragment_error";

    // Sandbox events
    public const string SandboxCreated = "sandbox_created";
    public const string SandboxStarted = "sandbox_started";
    public const string SandboxStopped = "sandbox_stopped";
    public const string SandboxError = "sandbox_error";

    // Chat events
    public const string ChatSubmit = "chat_submit";
    public const string ChatMessage = "chat_message";
    public const string ChatError = "chat_error";

    // Template events
    public const string TemplateSelected = "template_selected";
    public const string TemplateUsed = "template_used";

    // Model events
    public const string ModelSelected = "model_selected";
    public const string ModelUsed = "model_used";

    // User events
    public const string UserSignIn = "user_sign_in";
    public const string UserSignOut = "user_sign_out";
    public const string UserSignUp = "user_sign_up";

    // Agent events
    public const string AgentInvoked = "agent_invoked";
    public const string AgentCompleted = "agent_completed";
    public const string AgentFailed = "agent_failed";
}

/// <summary>
/// Analytics middleware for ASP.NET Core.
/// </summary>
public class AnalyticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsMiddleware(
        RequestDelegate next,
        IAnalyticsService analyticsService)
    {
        _next = next;
        _analyticsService = analyticsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Track page view
        var path = context.Request.Path ?? "unknown";
        var userId = context.User?.FindFirst("sub")?.Value;

        await _analyticsService.TrackPageViewAsync(path, new Dictionary<string, object>
        {
            ["method"] = context.Request.Method,
            ["path"] = path
        }, userId);

        await _next(context);
    }
}
