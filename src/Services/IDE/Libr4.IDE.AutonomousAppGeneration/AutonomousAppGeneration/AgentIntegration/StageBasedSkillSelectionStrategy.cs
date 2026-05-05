using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class StageBasedSkillSelectionStrategy : ISkillSelectionStrategy
{
    private readonly ISkillRegistry _registry;

    public StageBasedSkillSelectionStrategy(ISkillRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<string> SelectSkillIds(string stage, GenerationPlan? plan)
    {
        _ = plan;
        var normalized = stage.Trim().ToLowerInvariant();
        return _registry.List()
            .Where(s => s.ApplicableStages.Any(st =>
                string.Equals(st, normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(st, StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyDictionary<string, string> SelectSkillsWithReasons(string stage, GenerationPlan? plan)
    {
        _ = plan;
        var normalized = stage.Trim().ToLowerInvariant();
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in _registry.List())
        {
            var matchingStage = skill.ApplicableStages.FirstOrDefault(st =>
                string.Equals(st, normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(st, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchingStage))
            {
                var reason = string.Equals(matchingStage, normalized, StringComparison.OrdinalIgnoreCase)
                    ? $"stage_match:{normalized}"
                    : $"stage_contains:{normalized}";
                results[skill.Id] = reason;
            }
        }

        return results;
    }
}
