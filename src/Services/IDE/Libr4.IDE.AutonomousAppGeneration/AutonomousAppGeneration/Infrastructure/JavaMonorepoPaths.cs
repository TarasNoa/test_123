using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Path and package discovery for backend/ + frontend/ Java+React monorepos (any domain).
/// </summary>
public static class JavaMonorepoPaths
{
    private static readonly Regex PackageLine = new(
        @"^\s*package\s+([\w.]+)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex JpaEntityLine = new(
        @"extends\s+JpaRepository\s*<\s*(\w+)\s*,",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsJavaReactPlan(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;

    public static string InferBasePackage(IList<GeneratedFile> files)
    {
        var main = files.FirstOrDefault(f =>
            IsBackendMainJava(f.RelativePath)
            && (f.Content?.Contains("@SpringBootApplication", StringComparison.Ordinal) ?? false));
        if (main is not null)
            return ExtractPackage(main.Content) ?? "com.generated.app";

        var any = files.FirstOrDefault(f =>
            IsBackendMainJava(f.RelativePath)
            && !string.IsNullOrWhiteSpace(f.Content));
        return any is not null ? ExtractPackage(any.Content) ?? "com.generated.app" : "com.generated.app";
    }

    public static string BackendMainJava(string basePackage, string relativeFile) =>
        $"backend/src/main/java/{basePackage.Replace('.', '/')}/{relativeFile}";

    public static GeneratedFile? FindByFileName(IList<GeneratedFile> files, string fileName) =>
        files.FirstOrDefault(f =>
            f.RelativePath.Replace('\\', '/').EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<GeneratedFile> BackendJavaFiles(IList<GeneratedFile> files) =>
        files.Where(f => IsBackendMainJava(f.RelativePath));

    public static IEnumerable<GeneratedFile> BackendRepositories(IList<GeneratedFile> files) =>
        BackendJavaFiles(files).Where(f =>
            f.RelativePath.Contains("/repository/", StringComparison.OrdinalIgnoreCase)
            && f.RelativePath.EndsWith("Repository.java", StringComparison.OrdinalIgnoreCase));

    public static string? ExtractEntityNameFromRepository(string content)
    {
        var match = JpaEntityLine.Match(content ?? string.Empty);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? ExtractPackage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
        var match = PackageLine.Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool IsBackendMainJava(string relativePath)
    {
        var path = relativePath.Replace('\\', '/');
        return path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
               && path.Contains("/src/main/java/", StringComparison.OrdinalIgnoreCase)
               && path.EndsWith(".java", StringComparison.OrdinalIgnoreCase);
    }
}
