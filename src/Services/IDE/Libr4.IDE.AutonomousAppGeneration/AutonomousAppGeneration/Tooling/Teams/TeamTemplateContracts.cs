using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;

public sealed record TeamTemplateDefinition(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> AgentRoles,
    IReadOnlyList<string> SkillPackIds,
    IReadOnlyList<string> TriggerKeywords,
    UserRole MinimumRole);

public sealed record TeamTemplateResolution(
    bool Matched,
    TeamTemplateDefinition? Template,
    string Reason);

public interface ITeamTemplateRepository
{
    void Upsert(TeamTemplateDefinition definition);
    TeamTemplateDefinition? Get(string id);
    IReadOnlyList<TeamTemplateDefinition> List();
}

public interface ITeamTemplateResolver
{
    TeamTemplateResolution Resolve(string userRequest);
}
