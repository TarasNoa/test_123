namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Watchdog telemetry snapshot for an MCP lane.
/// </summary>
public sealed class McpLaneWatchdogSnapshot
{
    public string ProfileKey { get; init; } = string.Empty;
    public string Lane { get; init; } = string.Empty;
    public DateTime LastCheckTimeUtc { get; init; }
    public string Status { get; init; } = string.Empty; // "available" or "degraded"
    public string? BlockerCode { get; init; }
    public string? DiagnosticMessage { get; init; }
}

/// <summary>
/// Watchdog history entry for an MCP lane.
/// </summary>
public sealed class McpLaneWatchdogHistoryEntry
{
    public DateTime CheckTimeUtc { get; init; }
    public string Status { get; init; } = string.Empty; // "available" or "degraded"
    public string? BlockerCode { get; init; }
}

/// <summary>
/// Performs periodic watchdog checks on MCP lane preflight status.
/// </summary>
public interface IMcpLaneWatchdog
{
    /// <summary>
    /// Performs a watchdog check for all configured MCP lanes.
    /// </summary>
    void PerformWatchdogCheck();

    /// <summary>
    /// Gets the current watchdog snapshot for all MCP lanes.
    /// </summary>
    IReadOnlyList<McpLaneWatchdogSnapshot> GetSnapshot();

    /// <summary>
    /// Gets the watchdog history for a specific profile key.
    /// </summary>
    IReadOnlyList<McpLaneWatchdogHistoryEntry> GetHistory(string profileKey);
}
