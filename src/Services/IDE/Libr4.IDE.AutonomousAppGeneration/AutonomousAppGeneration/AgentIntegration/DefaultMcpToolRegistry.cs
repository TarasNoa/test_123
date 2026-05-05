using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultMcpToolRegistry : IMcpToolRegistry
{
    private readonly Dictionary<string, McpToolMetadata> _byName;

    public DefaultMcpToolRegistry()
    {
        var tools = new[]
        {
            new McpToolMetadata("mcp.list", "mcp-meta", "List available MCP tools (lazy discovery)",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "discovery", "meta" }),
            new McpToolMetadata("mcp.search", "mcp-meta", "Search MCP tools by query",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "discovery", "meta" }),
            new McpToolMetadata("mcp.describe", "mcp-meta", "Describe one MCP tool metadata",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "discovery", "meta" }),
            new McpToolMetadata("mcp.call", "mcp-meta", "Call MCP tool via meta-router",
                McpToolRiskLevel.Medium, McpExecutionLaneKind.Internal, new[] { "discovery", "meta" }),
            new McpToolMetadata("send_message", "libr4-agent-bridge", "Bridge message to another agent",
                McpToolRiskLevel.Medium, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("read_messages", "libr4-agent-bridge", "Read queued bridge messages",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("ack_message", "libr4-agent-bridge", "Acknowledge bridge message",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("reserve_task", "libr4-agent-bridge", "Reserve task lock",
                McpToolRiskLevel.Medium, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("release_task", "libr4-agent-bridge", "Release task lock",
                McpToolRiskLevel.Medium, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("heartbeat_task", "libr4-agent-bridge", "Heartbeat task lock",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("list_task_locks", "libr4-agent-bridge", "List task locks",
                McpToolRiskLevel.Low, McpExecutionLaneKind.Internal, new[] { "coordination" }),
            new McpToolMetadata("browser.smoke", "browser-lane", "Browser smoke test (navigate + screenshot)",
                McpToolRiskLevel.High, McpExecutionLaneKind.Browser, new[] { "ui", "e2e", "smoke" }),
            new McpToolMetadata("browser.auth", "browser-lane", "Browser auth flow test (login + screenshot)",
                McpToolRiskLevel.High, McpExecutionLaneKind.Browser, new[] { "ui", "e2e", "auth" }),
            new McpToolMetadata("n8n.workflow.test", "n8n-lane", "n8n workflow validation (safe mode)",
                McpToolRiskLevel.High, McpExecutionLaneKind.N8n, new[] { "workflow", "validation" }),
        };
        _byName = tools.ToDictionary(t => t.ToolName, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<McpToolMetadata> ListTools() => _byName.Values.ToList();

    public McpToolMetadata? FindTool(string toolName) =>
        _byName.TryGetValue(toolName, out var m) ? m : null;
}
