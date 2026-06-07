using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public static class ReviewRepairScopeHelper
{
    public static IReadOnlyList<GeneratedFile> SelectScopedFiles(
        IReadOnlyList<GeneratedFile> allFiles,
        IReadOnlyList<string> paths) =>
        allFiles
            .Where(f => paths.Any(p => PathMatches(f.RelativePath, p)))
            .ToList();

    public static string BuildRepairTask(IReadOnlyList<string> paths, string? notes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Human review requested fixes for the following files ONLY:");
        foreach (var path in paths)
            sb.AppendLine($"- {path}");
        if (!string.IsNullOrWhiteSpace(notes))
            sb.AppendLine($"Reviewer notes: {notes}");
        sb.AppendLine("Apply minimal surgical fixes. Do not modify files outside this scope unless required for compilation.");
        return sb.ToString();
    }

    public static bool PathMatches(string candidate, string target)
    {
        candidate = NormalizePath(candidate);
        target = NormalizePath(target);
        return candidate.Equals(target, StringComparison.OrdinalIgnoreCase)
               || candidate.EndsWith('/' + target, StringComparison.OrdinalIgnoreCase)
               || target.EndsWith('/' + candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
