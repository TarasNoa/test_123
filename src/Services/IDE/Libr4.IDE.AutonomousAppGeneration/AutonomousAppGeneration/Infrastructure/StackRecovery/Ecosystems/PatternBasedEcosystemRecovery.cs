using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>
/// Applies catalog-driven normalization for any matched ecosystem (languages + frameworks).
/// </summary>
public static class PatternBasedEcosystemRecovery
{
    public static int Normalize(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> warnings,
        bool autoFix,
        IReadOnlyList<EcosystemMatch>? matches = null)
    {
        matches ??= EcosystemMatcher.Match(plan, files.ToList());
        if (matches.Count == 0)
            return 0;

        warnings.Add($"ecosystems=[{string.Join(", ", matches.Select(m => $"{m.Profile.Id}({m.Score})"))}]");
        var fixes = 0;

        foreach (var match in matches)
            fixes += ApplyProfile(files, match.Profile, warnings, autoFix);

        return fixes;
    }

    public static int ApplyProfile(
        IList<GeneratedFile> files,
        EcosystemProfile profile,
        List<string> warnings,
        bool autoFix)
    {
        var fixes = 0;
        fixes += DeduplicateManifests(files, profile, warnings, autoFix);
        fixes += DeduplicateEntryPoints(files, profile, warnings, autoFix);
        fixes += DeduplicateNamedTypes(files, profile, warnings, autoFix);
        return fixes;
    }

    private static int DeduplicateManifests(
        IList<GeneratedFile> files,
        EcosystemProfile profile,
        List<string> warnings,
        bool autoFix)
    {
        var removed = 0;
        foreach (var manifest in profile.Manifests)
        {
            var matches = files
                .Where(f => PathMatchesManifest(f.RelativePath, manifest.FileName))
                .ToList();
            if (matches.Count <= 1 || manifest.AllowMultiple)
                continue;

            warnings.Add($"[{profile.Id}] Multiple {manifest.FileName}: {string.Join(", ", matches.Select(m => m.RelativePath))}");
            if (!autoFix)
                continue;

            var keep = matches
                .OrderBy(m => m.RelativePath.Count(c => c == '/'))
                .ThenBy(m => m.RelativePath.Length)
                .First();
            removed += RemoveAllExcept(files, matches, keep.RelativePath);
        }

        return removed;
    }

    private static int DeduplicateEntryPoints(
        IList<GeneratedFile> files,
        EcosystemProfile profile,
        List<string> warnings,
        bool autoFix)
    {
        var removed = 0;
        foreach (var rule in profile.EntryPoints)
        {
            var matches = files.Where(f => EntryPointRuleMatches(f, rule)).ToList();
            if (matches.Count <= 1)
                continue;

            warnings.Add($"[{profile.Id}] Multiple entry points ({rule.ContentMarkers.FirstOrDefault() ?? "path"}): {string.Join(", ", matches.Select(m => m.RelativePath))}");
            if (!autoFix)
                continue;

            var keep = matches
                .OrderByDescending(m => ScoreEntryPoint(m, rule))
                .ThenBy(m => m.RelativePath.Count(c => c == '/'))
                .First();
            removed += RemoveAllExcept(files, matches, keep.RelativePath);
        }

        return removed;
    }

    private static int DeduplicateNamedTypes(
        IList<GeneratedFile> files,
        EcosystemProfile profile,
        List<string> warnings,
        bool autoFix)
    {
        var removed = 0;
        foreach (var typeName in profile.DuplicateTypeNames)
        {
            var matches = files
                .Where(f => (f.Content?.Contains($"class {typeName}", StringComparison.Ordinal) == true
                             || f.Content?.Contains($"interface {typeName}", StringComparison.Ordinal) == true
                             || f.Content?.Contains($"def {typeName}", StringComparison.Ordinal) == true
                             || f.Content?.Contains($"function {typeName}", StringComparison.Ordinal) == true))
                .ToList();
            if (matches.Count <= 1)
                continue;

            warnings.Add($"[{profile.Id}] Multiple {typeName}: {string.Join(", ", matches.Select(m => m.RelativePath))}");
            if (!autoFix)
                continue;

            var keep = matches
                .OrderBy(m => m.RelativePath.Count(c => c == '/'))
                .ThenBy(m => m.RelativePath, StringComparer.OrdinalIgnoreCase)
                .First();
            removed += RemoveAllExcept(files, matches, keep.RelativePath);
        }

        return removed;
    }

    private static int ScoreEntryPoint(GeneratedFile file, EntryPointRule rule)
    {
        var score = rule.Priority;
        var path = file.RelativePath.Replace('\\', '/');
        foreach (var prefer in rule.PreferPathContains)
        {
            if (path.Contains(prefer, StringComparison.OrdinalIgnoreCase))
                score += 10;
        }

        return score;
    }

    private static int RemoveAllExcept(IList<GeneratedFile> files, List<GeneratedFile> matches, string keepPath)
    {
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (matches.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keepPath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    private static bool PathMatchesManifest(string path, string manifestName)
    {
        var file = Path.GetFileName(path.Replace('\\', '/'));
        return file.Equals(manifestName, StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(manifestName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EntryPointRuleMatches(GeneratedFile file, EntryPointRule rule)
    {
        if (rule.PathSuffixes.Count > 0
            && !rule.PathSuffixes.Any(s => file.RelativePath.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (rule.ContentMarkers.Count == 0)
            return rule.PathSuffixes.Count > 0;

        var content = file.Content ?? string.Empty;
        return rule.ContentMarkers.Any(m => content.Contains(m, StringComparison.Ordinal));
    }
}
