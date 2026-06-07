using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Collapses duplicate Java package trees (e.g. com.generated.banking vs com.mobilebankpro) to a single canonical root.
/// </summary>
public static class JavaPackageRootConsolidator
{
    private static readonly Regex JavaPackageRegex = new(
        @"^\s*package\s+([\w.]+)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public static IReadOnlyList<GeneratedFile> Consolidate(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return files;

        var javaFiles = files
            .Where(f => f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (javaFiles.Count < 2)
            return files;

        var roots = javaFiles
            .Select(ExtractPackageRoot)
            .Where(r => r.Length > 0)
            .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        if (roots.Count <= 1)
            return files;

        var canonical = ChooseCanonicalRoot(roots, plan);
        var dropRoots = roots.Where(r => !string.Equals(r, canonical, StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kept = new List<GeneratedFile>();
        foreach (var file in files)
        {
            if (!file.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                || !file.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            {
                kept.Add(file);
                continue;
            }

            var root = ExtractPackageRoot(file);
            if (dropRoots.Contains(root))
                continue;

            kept.Add(file);
        }

        return kept;
    }

    private static string ChooseCanonicalRoot(IReadOnlyList<string> roots, GenerationPlan plan)
    {
        var slug = ToPackageSlug(plan.ApplicationName);
        var preferred = $"com.{slug}";
        var match = roots.FirstOrDefault(r => string.Equals(r, preferred, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        return roots[0];
    }

    public static string ExtractPackageRoot(GeneratedFile file)
    {
        var path = file.RelativePath.Replace('\\', '/');
        const string marker = "/src/main/java/";
        var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return string.Empty;

        var after = path[(idx + marker.Length)..];
        var parts = after.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return string.Empty;

        return $"{parts[0]}.{parts[1]}";
    }

    private static string ToPackageSlug(string applicationName)
    {
        var chars = applicationName.Where(char.IsLetterOrDigit).ToArray();
        var slug = new string(chars).ToLowerInvariant();
        return string.IsNullOrEmpty(slug) ? "generatedapp" : slug;
    }
}
