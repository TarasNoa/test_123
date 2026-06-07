namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed record StackQualityGateProfile(
    string RecipeId,
    string DisplayName,
    IReadOnlyList<string> GateStageTokens,
    IReadOnlyList<string> Categories);

/// <summary>
/// Maps verify recipes to relevant quality-gate stage/category filters for the build diagnostics dashboard.
/// </summary>
public static class StackQualityGateProfileCatalog
{
    private static readonly string[] UniversalStageTokens =
    [
        "verify",
        "obscura",
        "pre_safety",
        "normalization",
        "review",
        "security",
        "consistency"
    ];

    private static readonly string[] UniversalCategories =
    [
        "review",
        "normalization",
        "generation"
    ];

    private static readonly IReadOnlyList<StackQualityGateProfile> Profiles =
    [
        Profile("calorie-vision", "CalorieVision", ["django", "python", "manage.py", "pip", "pytest", "solidjs", "vite", "npm", "frontend"], ["build", "execution"]),
        Profile("banking", "Banking", ["maven", "pom", "spring", "java", "gradle", "react", "npm", "frontend"], ["build", "execution"]),
        Profile("django", "Django", ["django", "python", "manage.py", "pip", "pytest"], ["build", "execution"]),
        Profile("fastapi", "FastAPI", ["fastapi", "uvicorn", "python", "pytest", "pip"], ["build", "execution"]),
        Profile("vite", "Vite", ["vite", "npm", "node", "frontend", "typescript", "javascript"], ["build", "execution"]),
        Profile("solidjs", "SolidJS", ["solidjs", "solid-js", "npm", "vite", "frontend"], ["build", "execution"]),
        Profile("nextjs", "Next.js", ["nextjs", "next.js", "npm", "node", "frontend"], ["build", "execution"]),
        Profile("spring-boot", "Spring Boot", ["maven", "pom", "spring", "java", "gradle"], ["build", "execution"]),
        Profile("dotnet", ".NET", ["dotnet", "csproj", "nuget", "aspnet", "csharp"], ["build", "execution"]),
        Profile("express", "Express", ["express", "npm", "node", "javascript"], ["build", "execution"]),
        Profile("generic-fallback", "Generic", [], ["build", "execution", "recovery"])
    ];

    public static IReadOnlyList<StackQualityGateProfile> All => Profiles;

    public static StackQualityGateProfile? TryGet(string? recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
            return null;

        return Profiles.FirstOrDefault(p =>
            p.RecipeId.Equals(recipeId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool MatchesFilter(
        StackQualityGateProfile? profile,
        string stage,
        string category,
        IReadOnlyList<string>? reasons = null)
    {
        if (profile is null)
            return true;

        if (MatchesAnyToken(stage, UniversalStageTokens) || MatchesAnyToken(category, UniversalCategories))
            return true;

        if (profile.GateStageTokens.Count > 0)
        {
            if (MatchesAnyToken(stage, profile.GateStageTokens))
                return true;

            if (reasons is not null && reasons.Any(r => MatchesAnyToken(r, profile.GateStageTokens)))
                return true;
        }

        if (profile.GateStageTokens.Count == 0
            && profile.Categories.Count > 0
            && profile.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return profile.RecipeId.Equals("generic-fallback", StringComparison.OrdinalIgnoreCase);
    }

    private static StackQualityGateProfile Profile(
        string id,
        string displayName,
        string[] stageTokens,
        string[] categories) =>
        new(id, displayName, stageTokens, categories);

    private static bool MatchesAnyToken(string value, IReadOnlyList<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return tokens.Any(token =>
            value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
