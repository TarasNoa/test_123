using System.Security.Cryptography;
using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public static class WorkspaceTrustHasher
{
    public static string Compute(string? projectWorkspacePath, string? tenantId, string fingerprint)
    {
        var key = !string.IsNullOrWhiteSpace(projectWorkspacePath)
            ? NormalizePath(projectWorkspacePath)
            : $"{tenantId ?? "default"}|{fingerprint}";

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToLowerInvariant();
        }
        catch
        {
            return path.Trim().ToLowerInvariant();
        }
    }
}

public static class WorkspaceTrustPolicyMapper
{
    public static AgentRuntime.Permissions.AgentPermissionMode ToPermissionMode(WorkspaceSandboxPolicy policy) =>
        policy switch
        {
            WorkspaceSandboxPolicy.Strict => AgentRuntime.Permissions.AgentPermissionMode.Plan,
            WorkspaceSandboxPolicy.Standard => AgentRuntime.Permissions.AgentPermissionMode.AcceptEdits,
            WorkspaceSandboxPolicy.Permissive => AgentRuntime.Permissions.AgentPermissionMode.BypassPermissions,
            _ => AgentRuntime.Permissions.AgentPermissionMode.AcceptEdits
        };

    public static bool DeniesCloudInference(WorkspaceHostMode hostMode) =>
        hostMode == WorkspaceHostMode.LocalOnly;
}
