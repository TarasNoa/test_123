namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public sealed class ExtensionHostOptions
{
    public const string SectionName = "AutonomousAppGeneration:ExtensionHost";

    public bool Enabled { get; set; } = true;

    /// <summary>Project-level extensions root (relative to workspace or absolute).</summary>
    public string ProjectExtensionsRoot { get; set; } = ".libr4/extensions";

    /// <summary>User-level extensions root under profile directory.</summary>
    public string UserExtensionsRoot { get; set; } = ".libr4/extensions";

    public int DefaultHookTimeoutMs { get; set; } = 15_000;

    public int DefaultToolTimeoutMs { get; set; } = 30_000;
}
