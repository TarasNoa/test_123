namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>
/// Production golden path: language + backend + optional frontend + deterministic build contract.
/// Tier 1 paths get deep remediation (Java banking model); Tier 2 get plan alignment + light handlers.
/// </summary>
public sealed record GoldenStackPath(
    string Id,
    string DisplayName,
    EcosystemSupportTier Tier,
    RemediationDepth RemediationDepth,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> BackendFrameworks,
    IReadOnlyList<string> FrontendFrameworks,
    string Layout,
    string RuntimeImage,
    IReadOnlyList<string> BuildCommands,
    IReadOnlyList<string> TestCommands,
    string ContractMarker);
