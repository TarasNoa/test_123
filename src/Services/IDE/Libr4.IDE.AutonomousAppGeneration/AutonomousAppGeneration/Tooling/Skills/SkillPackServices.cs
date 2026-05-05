using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;

public sealed class InMemorySkillPackRepository : ISkillPackRepository
{
    private static readonly SkillPackDefinition[] BuiltInSkillPacks =
    {
        new("web-development-pack", "1.0.0", "web", new[] { "openapi", "graphql-schema", "frontend-build" }, UserRole.ExternalUser),
        new("devops-pack", "1.0.0", "ops", new[] { "ci-cd", "deploy", "observability" }, UserRole.ExternalUser),
        new("research-pack", "1.0.0", "research", new[] { "benchmarking", "experiment-design", "analysis" }, UserRole.ExternalUser),
        new("security-pack", "1.0.0", "security", new[] { "security-audit", "threat-model", "policy-check" }, UserRole.ExternalUser),
        new("internal-ops-pack", "1.0.0", "ops", new[] { "incident-debug", "war-room-playbook" }, UserRole.InternalDeveloper)
    };

    private readonly Dictionary<string, SkillPackDefinition> _packs = new(StringComparer.OrdinalIgnoreCase);

    public InMemorySkillPackRepository()
    {
        foreach (var pack in BuiltInSkillPacks)
            _packs[pack.Id] = pack;
    }

    public void Upsert(SkillPackDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Skill pack id is required.", nameof(definition));
        _packs[definition.Id] = definition;
    }

    public SkillPackDefinition? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return _packs.TryGetValue(id, out var value) ? value : null;
    }

    public IReadOnlyList<SkillPackDefinition> List() => _packs.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
}

public sealed class SkillPackGovernanceService : ISkillPackGovernanceService
{
    private readonly IUserRoleProvider _userRoleProvider;

    public SkillPackGovernanceService(IUserRoleProvider userRoleProvider)
    {
        _userRoleProvider = userRoleProvider;
    }

    public bool CanUse(SkillPackDefinition definition)
    {
        var role = _userRoleProvider.GetCurrentRole();
        return role >= definition.MinimumRole;
    }
}
