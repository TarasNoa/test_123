using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;

public sealed record SkillPackDefinition(
    string Id,
    string Version,
    string Domain,
    IReadOnlyList<string> SkillIds,
    UserRole MinimumRole);

public interface ISkillPackRepository
{
    void Upsert(SkillPackDefinition definition);
    SkillPackDefinition? Get(string id);
    IReadOnlyList<SkillPackDefinition> List();
}

public interface ISkillPackGovernanceService
{
    bool CanUse(SkillPackDefinition definition);
}
