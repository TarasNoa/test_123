using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;

public static class WorkspaceSnapshotProvider
{
    public static string CaptureTree(string hostPath, int depth = 2)
    {
        if (string.IsNullOrWhiteSpace(hostPath) || !Directory.Exists(hostPath))
            return "(workspace unavailable)";

        var sb = new StringBuilder();
        AppendTree(sb, hostPath, ".", Math.Clamp(depth, 1, 6), 0);
        return sb.ToString().TrimEnd();
    }

    private static void AppendTree(StringBuilder sb, string absDir, string relDir, int maxDepth, int depth)
    {
        sb.Append(relDir).Append('/').AppendLine();
        if (depth >= maxDepth)
            return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(absDir).OrderBy(e => e, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return;
        }

        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (name.StartsWith('.') && name is not ".gitignore")
                continue;

            var childRel = relDir == "." ? name : $"{relDir}/{name}";
            if (Directory.Exists(entry))
                AppendTree(sb, entry, childRel, maxDepth, depth + 1);
            else
                sb.Append("  ").Append(childRel).AppendLine();
        }
    }
}
