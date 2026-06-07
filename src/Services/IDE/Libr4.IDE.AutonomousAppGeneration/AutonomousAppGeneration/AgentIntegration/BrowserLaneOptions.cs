namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class BrowserLaneOptions
{
    /// <summary>
    /// Browser lane provider: Obscura (native) or Node (legacy browser-mcp-server).
    /// </summary>
    public string Provider { get; set; } = "Obscura";

    /// <summary>
    /// Deprecation notice shown when Node provider is selected.
    /// </summary>
    public string? DeprecationNotice { get; set; } =
        "browser-mcp-server (Node) is deprecated. Set Mcp:BrowserLane:Provider to Obscura.";
}

public static class BrowserLaneOptionsExtensions
{
    public static bool UsesObscuraProvider(this BrowserLaneOptions options) =>
        options.Provider.Equals("Obscura", StringComparison.OrdinalIgnoreCase);

    public static bool UsesNodeProvider(this BrowserLaneOptions options) =>
        options.Provider.Equals("Node", StringComparison.OrdinalIgnoreCase);
}
