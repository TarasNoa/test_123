namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface ISpaceStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task InsertSpaceAsync(AgentSpace space, CancellationToken ct = default);
    Task<AgentSpace?> GetSpaceAsync(Guid spaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSpace>> ListSpacesAsync(string? ownerId, CancellationToken ct = default);
    Task InsertMemberAsync(SpaceMember member, CancellationToken ct = default);
    Task UpdateMemberAsync(SpaceMember member, CancellationToken ct = default);
    Task<SpaceMember?> GetMemberAsync(Guid spaceId, string memberId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaceMember>> ListMembersAsync(Guid spaceId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaceMember>> ListMembersByRunIdAsync(Guid runId, CancellationToken ct = default);
    Task<int> CountActiveMembersAsync(Guid spaceId, CancellationToken ct = default);
}
