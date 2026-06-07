namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>
/// Maps every catalog profile to Tier 1/2/3 support depth.
/// Tier 1 = golden paths; Tier 2 = standard handlers; Tier 3 = pattern-only.
/// </summary>
public static class DeveloperEcosystemTierRegistry
{
    private static readonly Lazy<IReadOnlyDictionary<string, EcosystemSupportTier>> ProfileTiers = new(BuildProfileTiers);

    public static EcosystemSupportTier GetTier(string profileId) =>
        ProfileTiers.Value.TryGetValue(profileId, out var tier) ? tier : EcosystemSupportTier.Tier3;

    public static RemediationDepth GetRemediationDepth(string profileId)
    {
        var tier = GetTier(profileId);
        return tier switch
        {
            EcosystemSupportTier.Tier1 => RemediationDepth.GoldenPath,
            EcosystemSupportTier.Tier2 => RemediationDepth.Standard,
            _ => RemediationDepth.PatternOnly
        };
    }

    public static bool IsGoldenPathProfile(string profileId) =>
        GoldenStackPathRegistry.AllPaths.Any(p =>
            p.Languages.Any(l => ProfileIdFromLanguage(l) == profileId)
            || p.BackendFrameworks.Any(fw => ProfileIdFromFramework(fw) == profileId)
            || p.FrontendFrameworks.Any(fw => ProfileIdFromFramework(fw) == profileId));

    public static IReadOnlyList<string> Tier1ProfileIds =>
        ProfileTiers.Value.Where(kv => kv.Value == EcosystemSupportTier.Tier1).Select(kv => kv.Key).ToList();

    public static IReadOnlyList<string> Tier2ProfileIds =>
        ProfileTiers.Value.Where(kv => kv.Value == EcosystemSupportTier.Tier2).Select(kv => kv.Key).ToList();

    public static IReadOnlyList<string> Tier3ProfileIds =>
        ProfileTiers.Value.Where(kv => kv.Value == EcosystemSupportTier.Tier3).Select(kv => kv.Key).ToList();

    private static IReadOnlyDictionary<string, EcosystemSupportTier> BuildProfileTiers()
    {
        var map = new Dictionary<string, EcosystemSupportTier>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in DeveloperEcosystemCatalog.AllProfiles)
            map[profile.Id] = InferTier(profile.Id);

        foreach (var path in GoldenStackPathRegistry.AllPaths)
        {
            foreach (var lang in path.Languages)
            {
                var id = ProfileIdFromLanguage(lang);
                if (!string.IsNullOrEmpty(id))
                    ApplyTier(map, id, path.Tier);
            }

            foreach (var fw in path.BackendFrameworks.Concat(path.FrontendFrameworks))
            {
                var id = ProfileIdFromFramework(fw);
                if (!string.IsNullOrEmpty(id))
                    ApplyTier(map, id, path.Tier);
            }
        }

        return map;
    }

    private static void ApplyTier(IDictionary<string, EcosystemSupportTier> map, string id, EcosystemSupportTier tier)
    {
        if (!map.TryGetValue(id, out var current) || tier < current)
            map[id] = tier;
    }

    private static EcosystemSupportTier InferTier(string profileId) => profileId switch
    {
        // Tier 1 languages & frameworks
        "java" or "csharp" or "python" or "typescript" or "javascript" or "go" or "rust" or "php"
            => EcosystemSupportTier.Tier1,
        "spring-boot" or "aspnet-core" or "fastapi" or "django" or "express" or "nestjs" or "nextjs"
            or "gin" or "axum" or "laravel" or "react" or "vue"
            => EcosystemSupportTier.Tier1,

        // Tier 2
        "kotlin" or "ruby" or "elixir" or "dart" or "swift" or "angular" or "nuxt" or "flutter"
            or "phoenix" or "rails" or "react-native" or "ktor" or "phoenix-liveview" or "swiftui"
            => EcosystemSupportTier.Tier2,

        // Tier 3 exotic
        "scala" or "haskell" or "ocaml" or "zig" or "nim" or "crystal" or "erlang" or "ada" or "cobol"
            or "clojure" or "fsharp" or "groovy" or "perl" or "lua" or "julia" or "elm" or "solidity"
            or "fortran" or "prolog" or "lisp" or "scheme" or "objectivec"
            => EcosystemSupportTier.Tier3,

        // Enterprise / niche — catalog detection only
        "apex" or "abap" or "vbnet" or "wolfram" or "matlab" or "verilog" or "vhdl"
            => EcosystemSupportTier.CatalogOnly,

        _ => EcosystemSupportTier.Tier3
    };

    private static string ProfileIdFromLanguage(string language) => language.ToLowerInvariant() switch
    {
        "c#" => "csharp",
        "go" => "go",
        "ruby on rails" => "ruby",
        _ => language.ToLowerInvariant().Replace(" ", "-")
    };

    private static string ProfileIdFromFramework(string framework) => framework.ToLowerInvariant() switch
    {
        "spring boot" => "spring-boot",
        "asp.net core" => "aspnet-core",
        "fastapi" => "fastapi",
        "django" => "django",
        "express" => "express",
        "nestjs" => "nestjs",
        "next.js" => "nextjs",
        "gin" => "gin",
        "axum" => "axum",
        "laravel" => "laravel",
        "react" => "react",
        "vue" => "vue",
        "ruby on rails" => "rails",
        "phoenix" => "phoenix",
        "flutter" => "flutter",
        "swiftui" => "swiftui",
        "react native" => "react-native",
        "angular" => "angular",
        "nuxt" => "nuxt",
        _ => framework.ToLowerInvariant().Replace(" ", "-").Replace(".", "")
    };
}
