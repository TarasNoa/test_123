using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public enum VerifySmokeKind
{
    None,
    Http,
    Browser
}

public sealed record VerifySmokeTarget(
    string Name,
    string Url,
    int Port,
    VerifySmokeKind Kind = VerifySmokeKind.Http);

public sealed record VerifyRecipe(
    string Id,
    string DisplayName,
    IReadOnlyList<string> InstallCommands,
    IReadOnlyList<string> BuildCommands,
    IReadOnlyList<string> TestCommands,
    IReadOnlyList<string> StartCommands,
    IReadOnlyList<VerifySmokeTarget> SmokeTargets,
    VerifySmokeKind SmokeKind);

public sealed record VerifyRecipeDetectionRequest(
    IReadOnlyList<GeneratedFile> Files,
    GenerationPlan? Plan = null,
    string? UserRequest = null,
    Guid? RunId = null,
    string? EvidenceRoot = null);

public sealed record VerifyRecipeDetectionResult(
    VerifyRecipe Recipe,
    string DetectionMethod,
    string? ManifestPath = null);
