namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Result of an MCP server preflight check.
/// </summary>
public sealed class McpServerPreflightResult
{
    public bool IsAvailable { get; init; }
    public string? BlockerCode { get; init; }
    public string? DiagnosticMessage { get; init; }

    public static McpServerPreflightResult Available() =>
        new() { IsAvailable = true };

    public static McpServerPreflightResult ServerMissing(string path) =>
        new()
        {
            IsAvailable = false,
            BlockerCode = "mcp_server_missing",
            DiagnosticMessage = $"MCP server executable not found: {path}"
        };

    public static McpServerPreflightResult ServerUnreachable(string path, Exception ex) =>
        new()
        {
            IsAvailable = false,
            BlockerCode = "mcp_server_unreachable",
            DiagnosticMessage = $"MCP server unreachable: {path} - {ex.Message}"
        };
}

/// <summary>
/// Performs preflight checks on MCP server configurations.
/// </summary>
public interface IMcpServerPreflight
{
    /// <summary>
    /// Checks if the configured MCP server for a given profile is available.
    /// </summary>
    McpServerPreflightResult CheckServerAvailability(string profileKey);
}
