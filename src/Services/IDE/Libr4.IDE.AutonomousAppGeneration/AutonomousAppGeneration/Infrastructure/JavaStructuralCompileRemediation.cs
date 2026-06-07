using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic structural fixes for recurring Java/Spring monorepo compile failures
/// (invalid pom, duplicate mains, orphan test packages).
/// </summary>
public static class JavaStructuralCompileRemediation
{
    public static int ApplyStructuralFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        string? executionLog)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return 0;

        var changed = 0;
        changed += FixDuplicatePomBuildSections(files);
        changed += RemoveDuplicateSpringBootMainClasses(files);
        changed += JavaMavenCompileRemediation.Apply(files, plan, executionLog);
        return changed;
    }

    private static int FixDuplicatePomBuildSections(IList<GeneratedFile> files)
    {
        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        var buildMatches = Regex.Matches(content, "<build>", RegexOptions.IgnoreCase);
        if (buildMatches.Count <= 1)
            return 0;

        var merged = Regex.Replace(
            content,
            "</build>\\s*<build>",
            string.Empty,
            RegexOptions.IgnoreCase);
        if (string.Equals(merged, content, StringComparison.Ordinal))
            return 0;

        files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
        return 1;
    }

    private static int RemoveDuplicateSpringBootMainClasses(IList<GeneratedFile> files)
    {
        var mains = files
            .Where(f => f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains("@SpringBootApplication", StringComparison.Ordinal) ?? false))
            .ToList();
        if (mains.Count <= 1)
            return 0;

        var keep = mains.FirstOrDefault(f =>
                       f.RelativePath.Contains("Application", StringComparison.OrdinalIgnoreCase))
                   ?? mains[0];
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            var f = files[i];
            if (!mains.Any(m => m.RelativePath.Equals(f.RelativePath, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (f.RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
                continue;
            files.RemoveAt(i);
            removed++;
        }

        return removed;
    }
}
