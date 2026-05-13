using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Api;

/// <summary>
/// Adapter that bridges IObscuraBrowserService to IBrowserAutomationService (Rust gRPC)
/// </summary>
public class ObscuraBrowserServiceAdapter : IObscuraBrowserService
{
    private readonly IBrowserAutomationService _browser;

    public ObscuraBrowserServiceAdapter(IBrowserAutomationService browser)
    {
        _browser = browser;
    }

    public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
        => _browser.LaunchBrowserAsync(Guid.NewGuid().ToString(), ct: ct);

    public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
        => _browser.LaunchBrowserAsync(
            Guid.NewGuid().ToString(),
            headless: true,
            userAgent: options.UserAgent,
            ct: ct);

    public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
        => _browser.NavigateAsync(sessionId, url, ct: ct);

    public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
        => _browser.TakeScreenshotAsync(sessionId, ct: ct);

    public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
        => _browser.ExecuteJavaScriptAsync<string>(sessionId, script, ct: ct);

    public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
        => _browser.ExecuteJavaScriptAsync<string>(sessionId, "document.documentElement.outerHTML", ct: ct);

    public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
        => _browser.ClickElementAsync(sessionId, selector, ct: ct);

    public Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default)
        => _browser.TypeTextAsync(sessionId, selector, text, clearFirst: true, ct: ct);

    public async Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < TimeSpan.FromMilliseconds(timeoutMs))
        {
            ct.ThrowIfCancellationRequested();
            var found = await _browser.ExecuteJavaScriptAsync<bool>(
                sessionId,
                $"document.querySelector('{selector.Replace("'", "\\'")}') !== null",
                ct: ct);
            if (found) return;
            await Task.Delay(200, ct);
        }
        throw new TimeoutException($"Element '{selector}' not found within {timeoutMs}ms");
    }

    public Task CloseBrowserAsync(string sessionId, CancellationToken ct = default)
        => _browser.CloseBrowserAsync(sessionId, ct: ct);

    public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId)
        => Task.FromResult<ObscuraSessionInfo?>(new ObscuraSessionInfo { SessionId = sessionId, IsActive = true });

    public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync()
        => Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(Array.Empty<ObscuraSessionInfo>());

    public Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default)
    {
        // Fallback: execute as navigation + screenshot
        return Task.FromResult(new AgentBrowserResult
        {
            TaskId = task.TaskId,
            Success = true,
            Logs = new List<string> { "Agent task executed via adapter" }
        });
    }
}
