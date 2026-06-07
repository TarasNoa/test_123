namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public sealed class WorkspaceTrustOptions
{
    public const string SectionName = "AutonomousAppGeneration:WorkspaceTrust";

    public bool Enabled { get; set; } = true;

    public string DbPath { get; set; } = ".logs/workspace-trust.db";

    /// <summary>When true, skip SQLite lookup and first-run prompts.</summary>
    public bool BypassTrustStore { get; set; }

    /// <summary>Explicit config override — bypasses store when set.</summary>
    public string? ForceSandboxPolicy { get; set; }

    /// <summary>Explicit config override — bypasses store when set.</summary>
    public string? ForceHostMode { get; set; }

    public WorkspaceSandboxPolicy DefaultSandboxPolicy { get; set; } = WorkspaceSandboxPolicy.Standard;

    public WorkspaceHostMode DefaultHostMode { get; set; } = WorkspaceHostMode.CloudAllowed;

    public bool TryGetForcedSandboxPolicy(out WorkspaceSandboxPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(ForceSandboxPolicy))
        {
            policy = default;
            return false;
        }

        return Enum.TryParse(ForceSandboxPolicy, ignoreCase: true, out policy);
    }

    public bool TryGetForcedHostMode(out WorkspaceHostMode hostMode)
    {
        if (string.IsNullOrWhiteSpace(ForceHostMode))
        {
            hostMode = default;
            return false;
        }

        return Enum.TryParse(ForceHostMode, ignoreCase: true, out hostMode);
    }
}
