using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public static class StackQualityGateFilter
{
    public static IReadOnlyList<BuildGateTimelineEntryDto> Apply(
        IReadOnlyList<BuildGateTimelineEntryDto> timeline,
        string? stackFilterRecipeId)
    {
        if (string.IsNullOrWhiteSpace(stackFilterRecipeId)
            || stackFilterRecipeId.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return timeline;
        }

        var profile = StackQualityGateProfileCatalog.TryGet(stackFilterRecipeId);
        if (profile is null)
            return timeline;

        return timeline
            .Where(g => StackQualityGateProfileCatalog.MatchesFilter(profile, g.Stage, g.Category, g.Reasons))
            .ToList();
    }

    public static IReadOnlyList<StackFilterOptionDto> BuildOptions(
        IReadOnlyList<BuildGateTimelineEntryDto> timeline,
        VerifyRecipeDetectionResult? detection)
    {
        var options = new List<StackFilterOptionDto>
        {
            new("all", "All gates", timeline.Count)
        };

        var detectedId = detection?.Recipe.Id;
        foreach (var profile in StackQualityGateProfileCatalog.All)
        {
            var count = timeline.Count(g =>
                StackQualityGateProfileCatalog.MatchesFilter(profile, g.Stage, g.Category, g.Reasons));
            if (count == 0 && !string.Equals(profile.RecipeId, detectedId, StringComparison.OrdinalIgnoreCase))
                continue;

            options.Add(new StackFilterOptionDto(profile.RecipeId, profile.DisplayName, count));
        }

        return options
            .GroupBy(o => o.RecipeId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(o => o.GateCount)
            .ThenBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
