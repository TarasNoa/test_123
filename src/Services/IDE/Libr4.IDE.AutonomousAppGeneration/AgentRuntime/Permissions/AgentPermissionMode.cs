namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;

public enum AgentPermissionMode
{
    Plan,
    AcceptEdits,
    BypassPermissions,
    Dangerous
}

public static class AgentPermissionModeExtensions
{
    public static AgentPermissionMode Parse(string? value) =>
        Enum.TryParse<AgentPermissionMode>(value, ignoreCase: true, out var mode)
            ? mode
            : AgentPermissionMode.BypassPermissions;

    public static bool AllowsMutatingTools(AgentPermissionMode mode) =>
        mode is AgentPermissionMode.AcceptEdits or AgentPermissionMode.BypassPermissions or AgentPermissionMode.Dangerous;

    public static bool AllowsBash(AgentPermissionMode mode) =>
        mode is AgentPermissionMode.BypassPermissions or AgentPermissionMode.Dangerous;
}
