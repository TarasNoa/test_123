using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Generates a frontend design brief before UI code generation.
/// </summary>
public sealed record FrontendDesignArtifact(
    string ArtifactId,
    string Version,
    IReadOnlyDictionary<string, string> DesignTokens,
    IReadOnlyDictionary<string, string> Palette,
    IReadOnlyDictionary<string, string> Typography,
    IReadOnlyDictionary<string, string> Components,
    IReadOnlyDictionary<string, string> Screens,
    IReadOnlyDictionary<string, string> Accessibility);

public sealed record FrontendDesignArtifactExport(
    string ArtifactId,
    string ArtifactPath,
    string Sha256,
    DateTime ExportedAtUtc);

public sealed record FrontendDesignPreplanResult(
    string BriefMarkdown,
    FrontendDesignArtifact Artifact,
    FrontendDesignArtifactExport? Export);

public interface IFrontendDesignPreplannerService
{
    bool ShouldRunFor(GenerationPlan plan);

    Task<FrontendDesignPreplanResult?> GenerateDesignAsync(
        string userRequest,
        GenerationPlan plan,
        CancellationToken ct = default);
}

