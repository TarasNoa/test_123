namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;

public sealed record SubagentProfile(
    string Id,
    string DisplayName,
    string DomainCategory,
    string Role,
    IReadOnlyList<string> CapabilityTags,
    IReadOnlyList<string> AllowedTools);

public interface ISubagentProfileRepository
{
    void Upsert(SubagentProfile profile);
    SubagentProfile? Get(string id);
    IReadOnlyList<SubagentProfile> List();
    IReadOnlyList<SubagentProfile> ListByRole(string role);
}

public interface ISubagentSelector
{
    IReadOnlyList<SubagentProfile> SelectByRoles(IReadOnlyList<string> roles);
}
