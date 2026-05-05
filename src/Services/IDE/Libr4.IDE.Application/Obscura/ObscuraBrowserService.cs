using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Stub implementation of Obscura browser service
/// </summary>
public class ObscuraBrowserService : IObscuraBrowserService
{
    private readonly ILogger<ObscuraBrowserService> _logger;
    private readonly Dictionary<string, ObscuraSessionInfo> _sessions = new();

    public ObscuraBrowserService(ILogger<ObscuraBrowserService> logger)
    {
        _logger = logger;
    }

    public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
    {
        return LaunchBrowserAsync(new ObscuraLaunchOptions(), ct);
    }

    public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new ObscuraSessionInfo
        {
            SessionId = sessionId,
            Port = options.Port,
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };
        _sessions[sessionId] = session;
        _logger.LogInformation("Launched Obscura browser session {SessionId}", sessionId);
        return Task.FromResult(sessionId);
    }

    public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.CurrentUrl = url;
            session.LastActivityAt = DateTime.UtcNow;
            _logger.LogInformation("Navigated session {SessionId} to {Url}", sessionId, url);
        }
        return Task.CompletedTask;
    }

    public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Taking screenshot for session {SessionId}", sessionId);
        // Return 1x1 transparent PNG
        return Task.FromResult(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    }

    public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
    {
        _logger.LogInformation("Executing JavaScript in session {SessionId}", sessionId);
        return Task.FromResult("{\"result\": null}");
    }

    public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
    {
        return Task.FromResult("<html><body>Stub content</body></html>");
    }

    public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
    {
        _logger.LogInformation("Clicking {Selector} in session {SessionId}", selector, sessionId);
        return Task.CompletedTask;
    }

    public Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default)
    {
        _logger.LogInformation("Typing into {Selector} in session {SessionId}", selector, sessionId);
        return Task.CompletedTask;
    }

    public Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CloseBrowserAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            _sessions.Remove(sessionId);
            _logger.LogInformation("Closed Obscura browser session {SessionId}", sessionId);
        }
        return Task.CompletedTask;
    }

    public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync()
    {
        return Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(_sessions.Values.ToList());
    }

    public Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default)
    {
        return Task.FromResult(new AgentBrowserResult
        {
            TaskId = task.TaskId,
            Success = true,
            FinalUrl = _sessions.TryGetValue(sessionId, out var s) ? s.CurrentUrl : null,
            Logs = new List<string> { "Stub execution completed" }
        });
    }
}
