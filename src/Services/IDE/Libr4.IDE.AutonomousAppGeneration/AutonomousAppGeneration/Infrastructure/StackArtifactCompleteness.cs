using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Detects whether generated artifacts satisfy the minimum monorepo/layout for the planned stack.
/// </summary>
public static class StackArtifactCompleteness
{
    public static bool MeetsPlanMinimum(GenerationPlan plan, IReadOnlyList<GeneratedFile> files)
    {
        if (files.Count == 0)
            return false;

        return StackPlanHeuristics.Classify(plan) switch
        {
            StackKind.JavaReactFullStack => MeetsJavaReactMinimum(files),
            StackKind.Java => MeetsJavaBackendMinimum(files),
            StackKind.DotNet => MeetsDotNetMinimum(files),
            StackKind.Python => MeetsPythonMinimum(files),
            StackKind.Node => MeetsNodeMinimum(files),
            _ => files.Count >= 5
        };
    }

    private const int MaxRelativePathLength = 200;

    private static readonly string[] PathPayloadSeparators =
    {
        "\\n", "/n/", "\r\n", "\n", "\r",
    };

    private static readonly Regex PlausibleFilePathRegex = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9_./\-]*\.[a-zA-Z0-9]{1,12}$|^[a-zA-Z0-9][a-zA-Z0-9_./\-]+/$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<GeneratedFile> NormalizeAndDeduplicate(IReadOnlyList<GeneratedFile> files)
    {
        var dict = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var repaired = RepairGeneratedFile(file);
            if (repaired is null)
                continue;

            var path = repaired.RelativePath;
            var content = repaired.Content ?? string.Empty;

            if (dict.TryGetValue(path, out var existing))
            {
                if (content.Length > (existing.Content?.Length ?? 0))
                    dict[path] = repaired;
            }
            else
            {
                dict[path] = repaired;
            }
        }

        return dict.Values.ToList();
    }

    /// <summary>
    /// Fixes LLM artifacts where file body was embedded in <see cref="GeneratedFile.RelativePath"/>
    /// (e.g. <c>tests/Foo.cs\n using Xunit;</c> or <c>src/Bar.cs/n// Description: ...</c>).
    /// </summary>
    public static GeneratedFile? RepairGeneratedFile(GeneratedFile file)
    {
        if (file is null)
            return null;

        var rawPath = file.RelativePath ?? string.Empty;
        var content = file.Content ?? string.Empty;
        var (pathPart, embedded) = SplitPathFromEmbeddedPayload(rawPath);
        if (!string.IsNullOrWhiteSpace(embedded))
        {
            var payload = NormalizeEmbeddedFileContent(embedded);
            if (string.IsNullOrWhiteSpace(content) || payload.Length > content.Length)
                content = payload;
        }

        var path = SanitizeRelativePath(pathPart);
        if (string.IsNullOrWhiteSpace(path) || !IsPlausibleFilePath(path))
            return null;

        return new GeneratedFile(path, InferLanguage(path, file.Language), content);
    }

    public static string SanitizeRelativePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var path = raw.Trim().Trim('"', '\'');
        var (pathPart, _) = SplitPathFromEmbeddedPayload(path);
        path = pathPart;

        var newline = path.IndexOfAny(new[] { '\r', '\n' });
        if (newline >= 0)
            path = path[..newline].Trim();

        path = path.Replace('\\', '/');
        while (path.StartsWith("./", StringComparison.Ordinal))
            path = path[2..];

        path = path.TrimEnd('/');
        if (path.Contains("..", StringComparison.Ordinal))
            return string.Empty;

        if (path.Length > MaxRelativePathLength)
            return string.Empty;

        return path;
    }

    public static bool IsPlausibleFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxRelativePathLength)
            return false;

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        foreach (var ch in path)
        {
            if (char.IsControl(ch))
                return false;
        }

        if (path.Contains(' ', StringComparison.Ordinal)
            || path.Contains(':', StringComparison.Ordinal)
            || path.Contains('*', StringComparison.Ordinal)
            || path.Contains('?', StringComparison.Ordinal)
            || path.Contains('<', StringComparison.Ordinal)
            || path.Contains('>', StringComparison.Ordinal)
            || path.Contains('|', StringComparison.Ordinal)
            || path.Contains('"', StringComparison.Ordinal))
            return false;

        return PlausibleFilePathRegex.IsMatch(path)
            || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || path.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase)
            || path.Equals("frontend/package.json", StringComparison.OrdinalIgnoreCase);
    }

    private static (string PathPart, string? Embedded) SplitPathFromEmbeddedPayload(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, null);

        var earliest = -1;
        var sepLength = 0;
        foreach (var sep in PathPayloadSeparators)
        {
            var idx = raw.IndexOf(sep, StringComparison.Ordinal);
            if (idx <= 0)
                continue;
            if (earliest < 0 || idx < earliest)
            {
                earliest = idx;
                sepLength = sep.Length;
            }
        }

        if (earliest < 0)
            return (raw, null);

        var pathPart = raw[..earliest].TrimEnd('\\', '/');
        var embedded = raw[(earliest + sepLength)..];
        return (pathPart, string.IsNullOrWhiteSpace(embedded) ? null : embedded);
    }

    private static string NormalizeEmbeddedFileContent(string embedded)
    {
        var text = embedded.Trim();
        text = text.Replace("\\n", "\n", StringComparison.Ordinal);
        text = text.Replace("\\t", "\t", StringComparison.Ordinal);
        text = text.Replace("\\\"", "\"", StringComparison.Ordinal);
        if (text.StartsWith("// Description:", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("/ Description:", StringComparison.OrdinalIgnoreCase))
        {
            var codeStart = text.IndexOf("\nusing ", StringComparison.Ordinal);
            if (codeStart < 0)
                codeStart = text.IndexOf("\nnamespace ", StringComparison.Ordinal);
            if (codeStart < 0)
                codeStart = text.IndexOf("\nimport ", StringComparison.Ordinal);
            if (codeStart < 0)
                codeStart = text.IndexOf("\npackage ", StringComparison.Ordinal);
            if (codeStart >= 0)
                text = text[(codeStart + 1)..];
        }

        return text.TrimStart('\r', '\n');
    }

    private static bool MeetsJavaReactMinimum(IReadOnlyList<GeneratedFile> files)
    {
        if (files.Count < 8)
            return false;

        var paths = files.Select(f => SanitizeRelativePath(f.RelativePath)).Where(p => p.Length > 0).ToList();
        var hasBackendPom = paths.Any(p => p.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase)
            || p.EndsWith("/backend/pom.xml", StringComparison.OrdinalIgnoreCase));
        var hasBackendJava = paths.Any(p =>
            p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            && p.EndsWith(".java", StringComparison.OrdinalIgnoreCase));
        var hasFrontendPackage = paths.Any(p =>
            p.Equals("frontend/package.json", StringComparison.OrdinalIgnoreCase)
            || p.EndsWith("/frontend/package.json", StringComparison.OrdinalIgnoreCase));
        var hasFrontendEntry = paths.Any(p =>
            p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
            && (p.EndsWith("App.tsx", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith("main.tsx", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith("index.tsx", StringComparison.OrdinalIgnoreCase)));

        return hasBackendPom && hasBackendJava && hasFrontendPackage && hasFrontendEntry;
    }

    private static bool MeetsJavaBackendMinimum(IReadOnlyList<GeneratedFile> files) =>
        files.Count >= 4
        && files.Any(f => SanitizeRelativePath(f.RelativePath).EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))
        && files.Any(f => SanitizeRelativePath(f.RelativePath).EndsWith(".java", StringComparison.OrdinalIgnoreCase));

    private static bool MeetsDotNetMinimum(IReadOnlyList<GeneratedFile> files) =>
        files.Count >= 6
        && files.Any(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        && files.Any(f => f.RelativePath.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase));

    private static bool MeetsPythonMinimum(IReadOnlyList<GeneratedFile> files) =>
        files.Count >= 4
        && files.Any(f =>
            f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase));

    private static bool MeetsNodeMinimum(IReadOnlyList<GeneratedFile> files) =>
        files.Count >= 4
        && files.Any(f => f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));

    private static string InferLanguage(string path, string fallback) =>
        string.IsNullOrWhiteSpace(fallback) || fallback == "plaintext"
            ? Path.GetExtension(path).TrimStart('.')
            : fallback;
}
