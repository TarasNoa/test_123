using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Obscura;

/// <summary>
/// Implementation of Obscura browser service
/// Manages Rust Obscura process and communicates via CDP
/// </summary>
public class ObscuraBrowserService : IObscuraBrowserService, IDisposable
{
    private readonly Dictionary<string, ObscuraSession> _sessions = new();
    private readonly ILogger<ObscuraBrowserService> _logger;
    private readonly string _obscuraBinaryPath;
    private readonly HttpClient _httpClient;
    private int _nextPort = 9222;
    private readonly object _lock = new();

    public ObscuraBrowserService(ILogger<ObscuraBrowserService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // Find Obscura binary
        _obscuraBinaryPath = FindObscuraBinary(configuration);
        _logger.LogInformation("Obscura binary found at: {Path}", _obscuraBinaryPath);
    }

    public async Task<string> LaunchBrowserAsync(CancellationToken ct = default)
    {
        return await LaunchBrowserAsync(new ObscuraLaunchOptions(), ct);
    }

    public async Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        
        lock (_lock)
        {
            if (options.Port == 0)
            {
                options.Port = _nextPort++;
            }
        }

        try
        {
            _logger.LogInformation(
                "Launching Obscura browser {SessionId} on port {Port} (stealth={Stealth})",
                sessionId, options.Port, options.StealthMode);

            // Build arguments
            var args = new List<string>
            {
                "serve",
                "--port", options.Port.ToString(),
                "--headless"
            };

            if (options.StealthMode)
            {
                args.Add("--stealth");
            }

            if (options.BlockTrackers)
            {
                args.Add("--block-trackers");
            }

            if (!string.IsNullOrEmpty(options.UserAgent))
            {
                args.Add("--user-agent");
                args.Add(options.UserAgent);
            }

            if (options.Viewport.HasValue)
            {
                args.Add("--viewport");
                args.Add($"{options.Viewport.Value.width}x{options.Viewport.Value.height}");
            }

            if (!string.IsNullOrEmpty(options.Proxy))
            {
                args.Add("--proxy");
                args.Add(options.Proxy);
            }

            // Start process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _obscuraBinaryPath,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_obscuraBinaryPath)
                }
            };

            var session = new ObscuraSession
            {
                SessionId = sessionId,
                Port = options.Port,
                Process = process,
                StartedAt = DateTime.UtcNow,
                IsActive = false,
                Options = options
            };

            // Start and wait for CDP ready
            process.Start();
            
            // Monitor startup
            _ = Task.Run(() => MonitorProcessOutput(sessionId, process), ct);

            // Wait for CDP to be ready
            await WaitForCdpReadyAsync(sessionId, options.Port, ct);

            session.IsActive = true;
            session.CdpWsUrl = $"ws://localhost:{options.Port}/devtools/browser";
            
            lock (_lock)
            {
                _sessions[sessionId] = session;
            }

            // Navigate to initial URL if specified
            if (!string.IsNullOrEmpty(options.InitialUrl))
            {
                await NavigateAsync(sessionId, options.InitialUrl, ct);
            }

            _logger.LogInformation(
                "Obscura browser {SessionId} launched successfully on port {Port}",
                sessionId, options.Port);

            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Obscura browser {SessionId}", sessionId);
            throw;
        }
    }

    public async Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
    {
        var session = GetSession(sessionId);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found");

        _logger.LogInformation("Navigating session {SessionId} to {Url}", sessionId, url);

        // Use CDP to navigate
        var result = await SendCdpCommandAsync(sessionId, "Page.navigate", new { url }, ct);
        
        session.CurrentUrl = url;
        session.LastActivityAt = DateTime.UtcNow;
        session.RequestCount++;

        _logger.LogInformation("Session {SessionId} navigated to {Url}", sessionId, url);
    }

    public async Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
    {
        var session = GetSession(sessionId);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found");

        _logger.LogDebug("Taking screenshot for session {SessionId}", sessionId);

        var result = await SendCdpCommandAsync(sessionId, "Page.captureScreenshot", new { format = "png" }, ct);
        
        var base64Data = result?.GetProperty("data").GetString();
        if (string.IsNullOrEmpty(base64Data))
        {
            throw new InvalidOperationException("Screenshot data is empty");
        }

        session.LastActivityAt = DateTime.UtcNow;
        return Convert.FromBase64String(base64Data);
    }

    public async Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
    {
        var session = GetSession(sessionId);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found");

        _logger.LogDebug("Executing JavaScript in session {SessionId}: {Script}", sessionId, script[..Math.Min(100, script.Length)]);

        var result = await SendCdpCommandAsync(sessionId, "Runtime.evaluate", new { expression = script }, ct);
        
        session.LastActivityAt = DateTime.UtcNow;
        
        var value = result?.GetProperty("result").GetProperty("value");
        return value?.ToString() ?? string.Empty;
    }

    public async Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
    {
        return await ExecuteJavaScriptAsync(sessionId, "document.documentElement.outerHTML", ct);
    }

    public async Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
    {
        var script = $@"
            (function() {{
                var el = document.querySelector('{selector.Replace("'", "\\'")}');
                if (el) {{
                    el.click();
                    return true;
                }}
                return false;
            }})()
        ";
        
        var result = await ExecuteJavaScriptAsync(sessionId, script, ct);
        
        if (result != "true")
        {
            throw new InvalidOperationException($"Element not found: {selector}");
        }
    }

    public async Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default)
    {
        var script = $@"
            (function() {{
                var el = document.querySelector('{selector.Replace("'", "\\'")}');
                if (el) {{
                    el.value = '{text.Replace("'", "\\'")}';
                    el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    el.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    return true;
                }}
                return false;
            }})()
        ";
        
        var result = await ExecuteJavaScriptAsync(sessionId, script, ct);
        
        if (result != "true")
        {
            throw new InvalidOperationException($"Element not found: {selector}");
        }
    }

    public async Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default)
    {
        var script = $@"
            new Promise((resolve) => {{
                var check = () => {{
                    if (document.querySelector('{selector.Replace("'", "\\'")}')) {{
                        resolve(true);
                    }} else if (Date.now() > start + {timeoutMs}) {{
                        resolve(false);
                    }} else {{
                        setTimeout(check, 100);
                    }}
                }};
                var start = Date.now();
                check();
            }})
        ";
        
        var result = await ExecuteJavaScriptAsync(sessionId, script, ct);
        
        if (result != "true")
        {
            throw new TimeoutException($"Element not found within {timeoutMs}ms: {selector}");
        }
    }

    public async Task CloseBrowserAsync(string sessionId, CancellationToken ct = default)
    {
        ObscuraSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
            {
                return;
            }
            _sessions.Remove(sessionId);
        }

        _logger.LogInformation("Closing Obscura browser {SessionId}", sessionId);

        try
        {
            // Send CDP close command
            await SendCdpCommandAsync(sessionId, "Browser.close", new { }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send CDP close command for session {SessionId}", sessionId);
        }

        // Kill process
        try
        {
            if (!session.Process.HasExited)
            {
                session.Process.Kill(true);
                await session.Process.WaitForExitAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to kill Obscura process for session {SessionId}", sessionId);
        }

        session.IsActive = false;
        _logger.LogInformation("Obscura browser {SessionId} closed", sessionId);
    }

    public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null) return Task.FromResult<ObscuraSessionInfo?>(null);

        return Task.FromResult(new ObscuraSessionInfo
        {
            SessionId = session.SessionId,
            Port = session.Port,
            CurrentUrl = session.CurrentUrl,
            StartedAt = session.StartedAt,
            LastActivityAt = session.LastActivityAt,
            IsActive = session.IsActive,
            MemoryUsageBytes = GetMemoryUsage(session),
            RequestCount = session.RequestCount
        });
    }

    public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync()
    {
        lock (_lock)
        {
            var sessions = _sessions.Values
                .Where(s => s.IsActive)
                .Select(s => new ObscuraSessionInfo
                {
                    SessionId = s.SessionId,
                    Port = s.Port,
                    CurrentUrl = s.CurrentUrl,
                    StartedAt = s.StartedAt,
                    LastActivityAt = s.LastActivityAt,
                    IsActive = s.IsActive,
                    MemoryUsageBytes = GetMemoryUsage(s),
                    RequestCount = s.RequestCount
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(sessions);
        }
    }

    public async Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new AgentBrowserResult
        {
            TaskId = task.TaskId,
            Logs = new List<string>(),
            Screenshots = new List<BrowserScreenshot>()
        };

        try
        {
            _logger.LogInformation(
                "Executing agent task {TaskId} with {ActionCount} actions",
                task.TaskId, task.Actions.Count);

            for (int i = 0; i < task.Actions.Count; i++)
            {
                var action = task.Actions[i];
                var actionId = $"{task.TaskId}-{i}";
                
                result.Logs.Add($"[{DateTime.UtcNow:HH:mm:ss}] Action {i + 1}/{task.Actions.Count}: {action.Type}");

                switch (action.Type)
                {
                    case BrowserActionType.Navigate:
                        if (!string.IsNullOrEmpty(action.Value))
                        {
                            await NavigateAsync(sessionId, action.Value, ct);
                        }
                        break;

                    case BrowserActionType.Click:
                        if (!string.IsNullOrEmpty(action.Selector))
                        {
                            await ClickAsync(sessionId, action.Selector, ct);
                        }
                        break;

                    case BrowserActionType.Type:
                        if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                        {
                            await TypeAsync(sessionId, action.Selector, action.Value, ct);
                        }
                        break;

                    case BrowserActionType.WaitForElement:
                        if (!string.IsNullOrEmpty(action.Selector))
                        {
                            await WaitForElementAsync(sessionId, action.Selector, action.WaitMs ?? 5000, ct);
                        }
                        break;

                    case BrowserActionType.Wait:
                        await Task.Delay(action.WaitMs ?? 1000, ct);
                        break;

                    case BrowserActionType.Screenshot:
                        if (task.TakeScreenshots)
                        {
                            var screenshot = await TakeScreenshotAsync(sessionId, ct);
                            result.Screenshots.Add(new BrowserScreenshot
                            {
                                ActionId = actionId,
                                Data = screenshot,
                                TakenAt = DateTime.UtcNow
                            });
                        }
                        break;

                    case BrowserActionType.ExecuteScript:
                        if (!string.IsNullOrEmpty(action.Value))
                        {
                            var scriptResult = await ExecuteJavaScriptAsync(sessionId, action.Value, ct);
                            result.Logs.Add($"Script result: {scriptResult}");
                        }
                        break;

                    case BrowserActionType.GetContent:
                        result.PageContent = await GetPageContentAsync(sessionId, ct);
                        break;
                }

                // Small delay between actions
                await Task.Delay(100, ct);
            }

            // Final screenshot
            if (task.TakeScreenshots)
            {
                var finalScreenshot = await TakeScreenshotAsync(sessionId, ct);
                result.Screenshots.Add(new BrowserScreenshot
                {
                    ActionId = $"{task.TaskId}-final",
                    Data = finalScreenshot,
                    TakenAt = DateTime.UtcNow
                });
            }

            // Get final URL
            var session = GetSession(sessionId);
            result.FinalUrl = session?.CurrentUrl;
            result.Success = true;

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Agent task {TaskId} completed in {Duration}s with {ScreenshotCount} screenshots",
                task.TaskId, result.Duration.TotalSeconds, result.Screenshots.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Error = ex.Message;
            result.Logs.Add($"ERROR: {ex.Message}");

            _logger.LogError(ex, "Agent task {TaskId} failed after {Duration}s", task.TaskId, result.Duration);
        }

        return result;
    }

    private string FindObscuraBinary(IConfiguration configuration)
    {
        // Check configuration first
        var configPath = configuration["Obscura:BinaryPath"];
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            return configPath;
        }

        // Search in common locations
        var possiblePaths = new[]
        {
            // Project location
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "obscura", "target", "release", "obscura"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "obscura", "target", "release", "obscura.exe"),
            
            // System PATH
            "obscura",
            "obscura.exe",
            
            // Docker/common install locations
            "/usr/local/bin/obscura",
            "/usr/bin/obscura",
            "/opt/obscura/obscura",
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path) || path == "obscura" || path == "obscura.exe")
            {
                return path;
            }
        }

        throw new FileNotFoundException("Obscura binary not found. Please install Obscura or set Obscura:BinaryPath configuration.");
    }

    private ObscuraSession? GetSession(string sessionId)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }
    }

    private async Task WaitForCdpReadyAsync(string sessionId, int port, CancellationToken ct)
    {
        var maxRetries = 50;
        var delay = 100;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:{port}/json/version", ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("CDP ready for session {SessionId} on port {Port}", sessionId, port);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "CDP not ready yet for session {SessionId}, attempt {Attempt}", sessionId, i + 1);
            }

            await Task.Delay(delay, ct);
        }

        throw new TimeoutException($"CDP did not become ready within {maxRetries * delay}ms");
    }

    private async Task<JsonElement?> SendCdpCommandAsync(string sessionId, string method, object @params, CancellationToken ct)
    {
        var session = GetSession(sessionId);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found");

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(session.CdpWsUrl!), ct);

        var command = new
        {
            id = Interlocked.Increment(ref session.CommandId),
            method,
            @params
        };

        var json = JsonSerializer.Serialize(command);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);

        // Receive response
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

        var doc = JsonDocument.Parse(response);
        return doc.RootElement;
    }

    private async Task MonitorProcessOutput(string sessionId, Process process)
    {
        try
        {
            while (!process.HasExited)
            {
                var output = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogDebug("Obscura [{SessionId}] {Output}", sessionId, output);
                }

                var error = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("Obscura [{SessionId}] ERROR: {Error}", sessionId, error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring Obscura process {SessionId}", sessionId);
        }
    }

    private long GetMemoryUsage(ObscuraSession session)
    {
        try
        {
            if (!session.Process.HasExited)
            {
                session.Process.Refresh();
                return session.Process.WorkingSet64;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to get memory usage for session {SessionId}", session.SessionId);
        }
        return 0;
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing ObscuraBrowserService, closing all sessions");
        
        var sessionIds = _sessions.Keys.ToList();
        foreach (var sessionId in sessionIds)
        {
            try
            {
                CloseBrowserAsync(sessionId).Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to close session {SessionId} during dispose", sessionId);
            }
        }

        _httpClient.Dispose();
    }

    private class ObscuraSession
    {
        public string SessionId { get; set; } = string.Empty;
        public int Port { get; set; }
        public Process Process { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public bool IsActive { get; set; }
        public string? CurrentUrl { get; set; }
        public string? CdpWsUrl { get; set; }
        public int RequestCount { get; set; }
        public long CommandId { get; set; }
        public ObscuraLaunchOptions Options { get; set; } = new();
    }
}
