namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public sealed class LspBridgeOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentIntegration:LspBridge";

    public bool Enabled { get; set; } = true;

    public bool EnableProcessServers { get; set; } = true;

    public int MaxDiagnosticsPerFile { get; set; } = 16;

    public int RequestTimeoutSeconds { get; set; } = 10;

    public Dictionary<string, LspServerLaunchProfile> Servers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LspServerLaunchProfile
{
    public string FileName { get; set; } = string.Empty;

    public List<string> Arguments { get; set; } = new();

    public string? WorkingDirectory { get; set; }

    public List<string> LanguageIds { get; set; } = new();
}
