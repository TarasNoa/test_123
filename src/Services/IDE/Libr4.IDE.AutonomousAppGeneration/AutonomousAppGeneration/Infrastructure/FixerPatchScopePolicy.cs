using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Path normalization and patch-scope filtering for the LLM fixer (shared with tests).
/// </summary>
public static class FixerPatchScopePolicy
{
    public static string NormalizePatchRelativePath(string path)
    {
        var p = path.Replace('\\', '/').Trim();
        while (p.StartsWith("./", StringComparison.Ordinal))
            p = p[2..];
        p = p.TrimStart('/');

        if (p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("tests/", StringComparison.OrdinalIgnoreCase))
            return p;

        foreach (var marker in new[] { "backend/", "frontend/" })
        {
            var idx = p.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return p[idx..];
        }

        foreach (var marker in new[] { "src/", "tests/" })
        {
            var idx = p.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                return p[idx..];
        }

        return p;
    }

    public static bool IsGenerationGapProductPath(string relativePath)
    {
        var path = NormalizePatchRelativePath(relativePath);
        if (path.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            return false;
        return path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
               || path.Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase)
               || path.Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/pom.xml", StringComparison.OrdinalIgnoreCase)
               || path.Equals("pom.xml", StringComparison.OrdinalIgnoreCase);
    }

    public static List<GeneratedFile> NormalizeParsedPatches(IReadOnlyList<GeneratedFile> parsed)
    {
        var list = new List<GeneratedFile>(parsed.Count);
        foreach (var file in parsed)
        {
            var path = NormalizePatchRelativePath(file.RelativePath);
            if (string.IsNullOrWhiteSpace(path))
                continue;
            list.Add(new GeneratedFile(path, file.Language, file.Content));
        }

        return list;
    }

    public static List<GeneratedFile> FilterPatches(
        IReadOnlyList<GeneratedFile> parsed,
        HashSet<string> allowed,
        IReadOnlyList<GeneratedFile> currentFiles,
        bool allowProductTreeFallback)
    {
        var strict = new List<GeneratedFile>();
        foreach (var file in parsed)
        {
            if (allowed.Contains(file.RelativePath))
            {
                strict.Add(file);
                continue;
            }

            var resolved = ResolvePatchToKnownPath(file, allowed, currentFiles);
            if (resolved is not null)
                strict.Add(resolved);
        }

        if (strict.Count > 0 || !allowProductTreeFallback)
            return strict;

        return parsed.Where(f => IsGenerationGapProductPath(f.RelativePath)).ToList();
    }

    private static GeneratedFile? ResolvePatchToKnownPath(
        GeneratedFile file,
        HashSet<string> allowed,
        IReadOnlyList<GeneratedFile> currentFiles)
    {
        var norm = file.RelativePath;
        var knownPaths = allowed
            .Concat(currentFiles.Select(f => f.RelativePath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var suffixMatches = knownPaths
            .Where(p => norm.EndsWith(p, StringComparison.OrdinalIgnoreCase)
                        || p.EndsWith(norm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Length)
            .ToList();

        if (suffixMatches.Count == 0)
            return null;

        var best = suffixMatches[0];
        return new GeneratedFile(best, file.Language, file.Content);
    }
}
