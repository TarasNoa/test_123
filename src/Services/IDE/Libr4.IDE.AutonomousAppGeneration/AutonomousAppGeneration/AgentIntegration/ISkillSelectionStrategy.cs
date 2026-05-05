using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ISkillSelectionStrategy
{
    IReadOnlyList<string> SelectSkillIds(string stage, GenerationPlan? plan);

    /// <summary>
    /// Select skills with selection reasons for provenance tracking.
    /// Returns a dictionary of skill ID -> selection reason.
    /// </summary>
    IReadOnlyDictionary<string, string> SelectSkillsWithReasons(string stage, GenerationPlan? plan);
}
