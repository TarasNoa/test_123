namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Service for managing Obscura headless browser instances
/// Obscura: 30MB RAM, 85ms page load, built-in anti-detection
/// </summary>
public interface IObscuraBrowserService
{
    /// <summary>
    /// Launch new Obscura browser instance
    /// </summary>
    Task<string> LaunchBrowserAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Launch browser with specific configuration
    /// </summary>
    Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Navigate to URL
    /// </summary>
    Task NavigateAsync(string sessionId, string url, CancellationToken ct = default);
    
    /// <summary>
    /// Take screenshot
    /// </summary>
    Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Execute JavaScript
    /// </summary>
    Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default);
    
    /// <summary>
    /// Get page content/HTML
    /// </summary>
    Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Click element
    /// </summary>
    Task ClickAsync(string sessionId, string selector, CancellationToken ct = default);
    
    /// <summary>
    /// Type text
    /// </summary>
    Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default);
    
    /// <summary>
    /// Wait for element
    /// </summary>
    Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default);
    
    /// <summary>
    /// Close browser
    /// </summary>
    Task CloseBrowserAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Get active session info
    /// </summary>
    Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId);
    
    /// <summary>
    /// List active sessions
    /// </summary>
    Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync();
    
    /// <summary>
    /// Execute agent task (high-level automation)
    /// </summary>
    Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default);
}

/// <summary>
/// Launch options for Obscura browser
/// </summary>
public class ObscuraLaunchOptions
{
    /// <summary>
    /// Port for CDP (Chrome DevTools Protocol)
    /// </summary>
    public int Port { get; set; } = 0; // 0 = auto-assign
    
    /// <summary>
    /// Enable stealth mode (anti-detection)
    /// </summary>
    public bool StealthMode { get; set; } = true;
    
    /// <summary>
    /// Block trackers and ads
    /// </summary>
    public bool BlockTrackers { get; set; } = true;
    
    /// <summary>
    /// User agent (null = randomize)
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Viewport size
    /// </summary>
    public (int width, int height)? Viewport { get; set; }
    
    /// <summary>
    /// Proxy URL (e.g., "http://proxy:8080")
    /// </summary>
    public string? Proxy { get; set; }
    
    /// <summary>
    /// Initial URL to navigate
    /// </summary>
    public string? InitialUrl { get; set; }
}

/// <summary>
/// Session information
/// </summary>
public class ObscuraSessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? CurrentUrl { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int RequestCount { get; set; }
}

/// <summary>
/// High-level task for agent automation
/// </summary>
public class AgentBrowserTask
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public List<BrowserAction> Actions { get; set; } = new();
    public int? TimeoutSeconds { get; set; }
    public bool TakeScreenshots { get; set; } = true;
}

/// <summary>
/// Single browser action
/// </summary>
public class BrowserAction
{
    public BrowserActionType Type { get; set; }
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public int? WaitMs { get; set; }
}

public enum BrowserActionType
{
    Navigate,
    Click,
    Type,
    WaitForElement,
    Wait,
    Screenshot,
    ExecuteScript,
    GetContent,
    Scroll
}

/// <summary>
/// Result of agent browser task
/// </summary>
public class AgentBrowserResult
{
    public string TaskId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FinalUrl { get; set; }
    public string? PageContent { get; set; }
    public List<BrowserScreenshot> Screenshots { get; set; } = new();
    public List<string> Logs { get; set; } = new();
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}

public class BrowserScreenshot
{
    public string ActionId { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime TakenAt { get; set; }
}
