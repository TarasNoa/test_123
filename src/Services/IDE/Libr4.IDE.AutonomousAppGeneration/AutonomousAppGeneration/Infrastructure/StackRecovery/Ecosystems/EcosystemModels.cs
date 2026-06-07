namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Depth of autonomous remediation support for a language or stack.</summary>
public enum EcosystemSupportTier
{
    /// <summary>Production golden paths (~80% market): deep normalize + compile remediation + plan alignment.</summary>
    Tier1 = 1,
    /// <summary>Secondary stacks (~15%): plan alignment + pattern recovery + light handlers.</summary>
    Tier2 = 2,
    /// <summary>Exotic stacks: catalog matching + pattern-based deduplication only.</summary>
    Tier3 = 3,
    /// <summary>Listed for detection only; no dedicated handler.</summary>
    CatalogOnly = 4
}

/// <summary>How aggressively Libr4 applies deterministic compile fixes for a stack.</summary>
public enum RemediationDepth
{
    GoldenPath,
    Standard,
    PatternOnly
}

public enum EcosystemCategory
{
    Language,
    BackendFramework,
    FrontendFramework,
    FullStack
}

public sealed record ManifestRule(
    string FileName,
    bool AllowMultiple = false,
    int Priority = 0);

public sealed record EntryPointRule(
    IReadOnlyList<string> PathSuffixes,
    IReadOnlyList<string> ContentMarkers,
    IReadOnlyList<string> PreferPathContains,
    int Priority = 0);

public sealed class EcosystemProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public EcosystemCategory Category { get; init; } = EcosystemCategory.Language;
    public IReadOnlyList<string> LanguageHints { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FrameworkHints { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FileExtensionHints { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ManifestRule> Manifests { get; init; } = Array.Empty<ManifestRule>();
    public IReadOnlyList<EntryPointRule> EntryPoints { get; init; } = Array.Empty<EntryPointRule>();
    public IReadOnlyList<string> DuplicateTypeNames { get; init; } = Array.Empty<string>();
    public int BasePriority { get; init; }
}

public sealed record EcosystemMatch(
    EcosystemProfile Profile,
    int Score,
    IReadOnlyList<string> Reasons);
