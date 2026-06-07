namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IVerifyRecipeRegistry
{
    IReadOnlyList<VerifyRecipe> AllRecipes { get; }

    VerifyRecipe? TryGet(string recipeId);

    Task<VerifyRecipeDetectionResult> DetectAsync(
        VerifyRecipeDetectionRequest request,
        CancellationToken ct = default);
}
