using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;

public sealed record SubagentRoutingDecision(
    bool Matched,
    string? TeamTemplateId,
    IReadOnlyList<string> AgentRoles,
    IReadOnlyList<string> AllowedSkillPackIds,
    string Reason);

public interface ISubagentRoutingService
{
    SubagentRoutingDecision Resolve(string userRequest);
}

public sealed class SubagentRoutingService : ISubagentRoutingService
{
    private readonly ITeamTemplateResolver _teamTemplateResolver;
    private readonly ISkillPackRepository _skillPackRepository;
    private readonly ISkillPackGovernanceService _skillPackGovernance;

    public SubagentRoutingService(
        ITeamTemplateResolver teamTemplateResolver,
        ISkillPackRepository skillPackRepository,
        ISkillPackGovernanceService skillPackGovernance)
    {
        _teamTemplateResolver = teamTemplateResolver;
        _skillPackRepository = skillPackRepository;
        _skillPackGovernance = skillPackGovernance;
    }

    public SubagentRoutingDecision Resolve(string userRequest)
    {
        var team = _teamTemplateResolver.Resolve(userRequest);
        if (!team.Matched || team.Template is null)
        {
            return new SubagentRoutingDecision(
                Matched: false,
                TeamTemplateId: null,
                AgentRoles: Array.Empty<string>(),
                AllowedSkillPackIds: Array.Empty<string>(),
                Reason: team.Reason);
        }

        var allowedSkillPacks = team.Template.SkillPackIds
            .Select(id => _skillPackRepository.Get(id))
            .Where(p => p is not null)
            .Cast<SkillPackDefinition>()
            .Where(_skillPackGovernance.CanUse)
            .Select(p => p.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SubagentRoutingDecision(
            Matched: true,
            TeamTemplateId: team.Template.Id,
            AgentRoles: team.Template.AgentRoles.ToArray(),
            AllowedSkillPackIds: allowedSkillPacks,
            Reason: team.Reason);
    }
}
