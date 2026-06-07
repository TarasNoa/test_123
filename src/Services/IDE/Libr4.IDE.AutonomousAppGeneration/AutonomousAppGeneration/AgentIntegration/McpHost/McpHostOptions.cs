namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public sealed class McpHostOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentIntegration:McpHost";

    /// <summary>Route MCP invocations through unified per-run host manager.</summary>
    public bool EnableUnifiedHost { get; set; } = true;

    public bool EnableStdioTransport { get; set; } = true;

    public bool EnableSseTransport { get; set; } = true;

    public int RunSessionIdleTimeoutMinutes { get; set; } = 30;

    public int DiscoveryTimeoutSeconds { get; set; } = 8;

    /// <summary>Remote MCP servers exposed via HTTP SSE transport.</summary>
    public Dictionary<string, McpSseServerProfile> SseServers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class McpSseServerProfile
{
    public string BaseUrl { get; set; } = string.Empty;

    public string MessagePath { get; set; } = "/message";

    public string? ApiKeyHeader { get; set; }

    public string? ApiKey { get; set; }
}

public enum McpHostTransportKind
{
    Stdio,
    Sse,
    Internal
}
