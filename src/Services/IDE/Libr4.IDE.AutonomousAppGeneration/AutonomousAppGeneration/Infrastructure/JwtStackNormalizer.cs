using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Aggressive consolidation of duplicate JWT/auth implementation files after multi-pass generation.
/// </summary>
public static class JwtStackNormalizer
{
    private static readonly string[] ImplementationPatterns =
    [
        "JwtTokenProvider",
        "JwtService",
        "JwtUtil",
        "TokenProvider",
        "JwtAuthenticationFilter",
        "JwtAuthFilter",
        "JwtGenerator"
    ];

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return 0;

        var removed = 0;
        removed += ConsolidateRole(files, "config", IsSecurityConfig, ScoreConfig);
        removed += ConsolidateRole(files, "provider", IsJwtProvider, ScoreProvider);
        removed += ConsolidateRole(files, "filter", IsJwtFilter, ScoreFilter);
        return removed;
    }

    private static int ConsolidateRole(
        IList<GeneratedFile> files,
        string role,
        Func<GeneratedFile, bool> matcher,
        Func<GeneratedFile, int> scorer)
    {
        var matches = files.Where(f =>
                f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
                && matcher(f))
            .ToList();

        if (matches.Count <= 1)
            return 0;

        var keep = matches.OrderByDescending(scorer).ThenBy(m => m.RelativePath, StringComparer.OrdinalIgnoreCase).First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (matches.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    private static bool IsSecurityConfig(GeneratedFile f) =>
        (f.Content?.Contains("@EnableWebSecurity", StringComparison.Ordinal) == true
         || f.Content?.Contains("@Configuration", StringComparison.Ordinal) == true)
        && (f.RelativePath.Contains("Security", StringComparison.OrdinalIgnoreCase)
            || (f.Content?.Contains("SecurityFilterChain", StringComparison.Ordinal) == true));

    private static bool IsJwtProvider(GeneratedFile f)
    {
        var name = Path.GetFileNameWithoutExtension(f.RelativePath);
        return ImplementationPatterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase))
               || (f.Content?.Contains("generateToken", StringComparison.OrdinalIgnoreCase) == true
                   && f.Content.Contains("JWT", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsJwtFilter(GeneratedFile f) =>
        f.RelativePath.Contains("Filter", StringComparison.OrdinalIgnoreCase)
        && (f.Content?.Contains("Jwt", StringComparison.OrdinalIgnoreCase) == true
            || f.Content?.Contains("Bearer", StringComparison.OrdinalIgnoreCase) == true);

    private static int ScoreConfig(GeneratedFile f)
    {
        var score = 0;
        if (f.Content?.Contains("SecurityFilterChain", StringComparison.Ordinal) == true) score += 40;
        if (f.RelativePath.Contains("/security/", StringComparison.OrdinalIgnoreCase)) score += 30;
        if (f.Content?.Contains("addFilterBefore", StringComparison.OrdinalIgnoreCase) == true) score += 20;
        score -= f.RelativePath.Count(c => c == '/');
        return score;
    }

    private static int ScoreProvider(GeneratedFile f)
    {
        var score = 0;
        var name = Path.GetFileNameWithoutExtension(f.RelativePath);
        if (name.Contains("JwtTokenProvider", StringComparison.OrdinalIgnoreCase)) score += 50;
        if (name.Contains("TokenService", StringComparison.OrdinalIgnoreCase)) score += 35;
        if (f.RelativePath.Contains("/security/", StringComparison.OrdinalIgnoreCase)) score += 25;
        if (f.Content?.Contains("interface", StringComparison.Ordinal) == true) score -= 10;
        if (f.Content?.Contains("implements", StringComparison.Ordinal) == true) score += 15;
        score -= f.RelativePath.Count(c => c == '/');
        return score;
    }

    private static int ScoreFilter(GeneratedFile f)
    {
        var score = 0;
        if (f.Content?.Contains("OncePerRequestFilter", StringComparison.Ordinal) == true) score += 40;
        if (f.RelativePath.Contains("/security/", StringComparison.OrdinalIgnoreCase)) score += 25;
        score -= f.RelativePath.Count(c => c == '/');
        return score;
    }
}
