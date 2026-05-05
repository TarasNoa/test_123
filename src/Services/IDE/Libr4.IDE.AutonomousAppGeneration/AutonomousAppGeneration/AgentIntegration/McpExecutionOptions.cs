namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class McpServerLaunchProfile
{
    public string FileName { get; set; } = "python";
    public List<string> Arguments { get; set; } = new();
    public string? WorkingDirectory { get; set; }
}

public sealed class McpExecutionOptions
{
    public bool EnableStdioTransport { get; set; }

    public int DefaultTimeoutMs { get; set; } = 60_000;

    public int MaxToolCallsPerRun { get; set; } = 128;

    public Dictionary<string, int> LaneMaxCalls { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["browser"] = 32,
        ["n8n"] = 32,
        ["workflow"] = 64,
        ["internal"] = 256,
    };

    public Dictionary<string, McpServerLaunchProfile> ServerProfiles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Limits concurrent MCP invocations that are not tied to an app-generation run (HTTP standalone).
    /// 0 = unlimited.
    /// </summary>
    public int MaxConcurrentStandaloneInvocations { get; set; } = 8;

    /// <summary>Enterprise kill-switch: reject all tools routed to the Browser lane.</summary>
    public bool KillSwitchBrowserLane { get; set; }

    /// <summary>Kill-switch for n8n / workflow automation lane.</summary>
    public bool KillSwitchN8nLane { get; set; }

    /// <summary>Kill-switch for generic workflow lane (reserved tools).</summary>
    public bool KillSwitchWorkflowLane { get; set; }

    /// <summary>Browser lane profile configuration.</summary>
    public Dictionary<string, BrowserLaneProfile> BrowserProfiles { get; set; } = new();

    /// <summary>n8n lane profile configuration.</summary>
    public Dictionary<string, N8nLaneProfile> N8nProfiles { get; set; } = new();

    /// <summary>
    /// When true, unavailable MCP servers result in degraded lane mode (no crash, audit entry with blocker code).
    /// When false, missing servers cause hard failures.
    /// </summary>
    public bool EnableDeterministicFallback { get; set; } = true;

    /// <summary>
    /// Maximum number of watchdog history entries to retain per MCP lane profile.
    /// </summary>
    public int WatchdogHistoryDepth { get; set; } = 100;
}

public sealed class BrowserLaneProfile
{
    public string ProfileType { get; set; } = "smoke"; // smoke, auth
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 30_000;
    public string? ScreenshotPath { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}

public sealed class N8nLaneProfile
{
    public string ProfileType { get; set; } = "workflow_test"; // workflow_test
    public string WorkflowId { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 60_000;
    public bool SafeMode { get; set; } = true;
    public Dictionary<string, string> Environment { get; set; } = new();
}
