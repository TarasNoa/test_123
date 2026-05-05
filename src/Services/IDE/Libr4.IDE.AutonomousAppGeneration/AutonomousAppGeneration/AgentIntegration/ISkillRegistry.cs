namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ISkillRegistry
{
    IReadOnlyList<SkillDefinition> List();

    SkillDefinition? Find(string skillId);
}
