using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Trims bloated frontend test/spec duplicates while keeping app entrypoints and manifests.
/// </summary>
public static class FrontendArtifactPruner
{
    public static IReadOnlyList<GeneratedFile> Prune(IReadOnlyList<GeneratedFile> files, int maxFrontendFiles = 45)
    {
        var frontend = files
            .Where(f => f.RelativePath.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (frontend.Count <= maxFrontendFiles)
            return files;

        var essential = frontend.Where(IsEssential).ToList();
        var tests = frontend.Where(f => IsTestArtifact(f.RelativePath)).ToList();
        var rest = frontend.Where(f => !IsEssential(f) && !IsTestArtifact(f.RelativePath)).ToList();

        var budget = Math.Max(0, maxFrontendFiles - essential.Count);
        var testsTake = Math.Min(tests.Count, Math.Max(4, budget / 4));
        var restTake = Math.Max(0, budget - testsTake);

        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in essential)
            selected.Add(f.RelativePath);
        foreach (var f in tests.Take(testsTake))
            selected.Add(f.RelativePath);
        foreach (var f in rest.Take(restTake))
            selected.Add(f.RelativePath);

        return files
            .Where(f => !f.RelativePath.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
                        || selected.Contains(f.RelativePath))
            .ToList();
    }

    private static bool IsEssential(GeneratedFile file)
    {
        var p = file.RelativePath.Replace('\\', '/').ToLowerInvariant();
        if (p.EndsWith("package.json") || p.EndsWith("package-lock.json"))
            return true;
        if (p.Contains("vite.config") || p.Contains("tsconfig"))
            return true;
        if (p.EndsWith("/src/main.tsx") || p.EndsWith("/src/app.tsx") || p.EndsWith("/src/index.tsx"))
            return true;
        if (p.EndsWith("/index.html"))
            return true;
        return false;
    }

    private static bool IsTestArtifact(string path)
    {
        var p = path.Replace('\\', '/').ToLowerInvariant();
        return p.Contains("/test/") || p.Contains("/tests/") || p.Contains("/__tests__/")
               || p.EndsWith(".test.ts") || p.EndsWith(".test.tsx") || p.EndsWith(".spec.ts") || p.EndsWith(".spec.tsx");
    }
}
