using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

public static class EcosystemMatcher
{
    public const int MinMatchScore = 12;
    public const int MaxProfilesPerRun = 12;

    public static IReadOnlyList<EcosystemMatch> Match(GenerationPlan plan, IReadOnlyList<GeneratedFile> files)
    {
        var blob = BuildSignalBlob(plan);
        var matches = new List<EcosystemMatch>();

        foreach (var profile in DeveloperEcosystemCatalog.AllProfiles)
        {
            var (score, reasons) = ScoreProfile(profile, blob, files);
            if (score >= MinMatchScore)
                matches.Add(new EcosystemMatch(profile, score, reasons));
        }

        return matches
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.Profile.BasePriority)
            .Take(MaxProfilesPerRun)
            .ToList();
    }

    private static string BuildSignalBlob(GenerationPlan plan)
    {
        var parts = new List<string>
        {
            plan.ApplicationName,
            plan.ApplicationDescription,
            plan.RuntimeImage ?? string.Empty
        };
        parts.AddRange(plan.TechStack.Languages);
        parts.AddRange(plan.TechStack.Frameworks);
        parts.AddRange(plan.TechStack.Databases);
        parts.AddRange(plan.TechStack.Infrastructure);
        parts.AddRange(plan.BuildCommands);
        parts.AddRange(plan.TestCommands);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    private static (int Score, List<string> Reasons) ScoreProfile(
        EcosystemProfile profile,
        string blob,
        IReadOnlyList<GeneratedFile> files)
    {
        var score = profile.BasePriority;
        var reasons = new List<string>();

        foreach (var hint in profile.LanguageHints)
        {
            if (ContainsToken(blob, hint))
            {
                score += 14;
                reasons.Add($"lang:{hint}");
            }
        }

        foreach (var hint in profile.FrameworkHints)
        {
            if (ContainsToken(blob, hint))
            {
                score += 18;
                reasons.Add($"fw:{hint}");
            }
        }

        foreach (var ext in profile.FileExtensionHints)
        {
            if (files.Any(f => f.RelativePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                score += 6;
                reasons.Add($"ext:{ext}");
            }
        }

        foreach (var manifest in profile.Manifests)
        {
            if (files.Any(f => PathMatchesManifest(f.RelativePath, manifest.FileName)))
            {
                score += 10;
                reasons.Add($"manifest:{manifest.FileName}");
            }
        }

        foreach (var rule in profile.EntryPoints)
        {
            if (files.Any(f => EntryPointRuleMatches(f, rule)))
            {
                score += 8;
                reasons.Add($"entry:{profile.Id}");
                break;
            }
        }

        return (score, reasons);
    }

    private static bool ContainsToken(string blob, string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return false;
        var h = hint.Trim().ToLowerInvariant();
        if (h.Length <= 2)
            return blob.Contains($" {h} ", StringComparison.Ordinal) || blob.StartsWith($"{h} ", StringComparison.Ordinal);
        return blob.Contains(h, StringComparison.Ordinal);
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
