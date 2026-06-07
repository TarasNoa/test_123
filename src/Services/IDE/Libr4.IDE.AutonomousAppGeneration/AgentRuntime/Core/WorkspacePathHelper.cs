namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

internal static class WorkspacePathHelper
{
    public static string ResolveHostPath(string workspaceRoot, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').Trim().TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal is not allowed");

        var abs = Path.GetFullPath(Path.Combine(workspaceRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(workspaceRoot);
        if (!abs.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Path escapes workspace root");

        return abs;
    }

    public static string ToRelative(string workspaceRoot, string absolutePath)
    {
        var root = Path.GetFullPath(workspaceRoot);
        var abs = Path.GetFullPath(absolutePath);
        if (!abs.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            return absolutePath;

        var rel = abs[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace('\\', '/');
    }
}
