namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>
/// Catalog of ~50 languages and ~70 popular frameworks for pattern-based artifact recovery.
/// </summary>
public static partial class DeveloperEcosystemCatalog
{
    private static readonly Lazy<IReadOnlyList<EcosystemProfile>> All = new(() =>
        BuildLanguageProfiles()
            .Concat(BuildFrameworkProfiles())
            .Concat(BuildExtendedFrameworkProfiles())
            .Concat(BuildProductionStackProfiles())
            .ToList());

    public static IReadOnlyList<EcosystemProfile> AllProfiles => All.Value;

    public static int LanguageCount => All.Value.Count(p => p.Category == EcosystemCategory.Language);

    public static int FrameworkCount => All.Value.Count(p => p.Category != EcosystemCategory.Language);

    public static EcosystemProfile? FindById(string id) =>
        All.Value.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
