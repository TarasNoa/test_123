namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface IAgentSpaceService
{
    Task<AgentSpace> CreateSpaceAsync(CreateSpaceRequest request, CancellationToken ct = default);
    Task<AgentSpaceDetail?> GetSpaceDetailAsync(Guid spaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentSpace>> ListSpacesAsync(string? ownerId, CancellationToken ct = default);
    Task<SpaceMember> SpawnAgentAsync(Guid spaceId, SpawnSpaceAgentRequest request, CancellationToken ct = default);
    Task<MergeSpaceMemberResult> MergeMemberAsync(Guid spaceId, string memberId, CancellationToken ct = default);
    Task<WorktreeDirectoryListing?> ListWorktreeFilesAsync(
        Guid spaceId,
        string memberId,
        string? relativePath = null,
        CancellationToken ct = default);

    Task<GitMergePreview?> PreviewMergeAsync(Guid spaceId, string memberId, CancellationToken ct = default);
}
