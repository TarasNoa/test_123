namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public enum SpaceMemberRole
{
    Implementer,
    Explorer,
    Verifier,
    Computer
}

public enum SpaceMemberStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Removed
}

public sealed record AgentSpace(
    Guid SpaceId,
    string Name,
    string? RepositoryUrl,
    string BaseBranch,
    string OwnerId,
    string SharedMemoryScope,
    string? McpProfile,
    DateTime CreatedAtUtc,
    string RootPath,
    string IntegrationBranch);

public sealed record SpaceMember(
    string MemberId,
    Guid SpaceId,
    SpaceMemberRole Role,
    Guid? RunId,
    string WorktreePath,
    string BranchName,
    SpaceMemberStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? LastError);

public sealed record CreateSpaceRequest(
    string Name,
    string? RepositoryUrl,
    string? BaseBranch,
    string? OwnerId,
    string? McpProfile,
    string? UserRequest);

public sealed record SpawnSpaceAgentRequest(
    SpaceMemberRole Role,
    string? Task,
    Guid? RunId,
    bool BindToIntegrationWorktree = false);

public sealed record SpaceOrchestrationRequest(
    string? ExplorerTask,
    string? ImplementerTask,
    string? VerifierTask,
    Guid? ExplorerRunId = null,
    Guid? ImplementerRunId = null,
    Guid? VerifierRunId = null,
    int ContextReadyTimeoutSeconds = 30,
    bool SkipVerifier = false);

public sealed record SpaceOrchestrationResult(
    Guid SpaceId,
    SpaceMember Explorer,
    SpaceMember Implementer,
    SpaceMember? Verifier,
    bool ContextReady,
    string Stage,
    IReadOnlyList<SpaceContextEvent> Timeline);

public sealed record AgentSpaceDetail(
    AgentSpace Space,
    IReadOnlyList<SpaceMember> Members,
    IReadOnlyList<SpaceContextEvent> RecentContext);

public sealed record MergeSpaceMemberResult(
    bool Success,
    string? Output,
    IReadOnlyList<string> Conflicts,
    string IntegrationBranch);

public sealed record WorktreeFileEntry(
    string Name,
    string RelativePath,
    bool IsDirectory,
    long? SizeBytes);

public sealed record WorktreeDirectoryListing(
    string WorktreePath,
    string RelativePath,
    IReadOnlyList<WorktreeFileEntry> Entries);

public sealed record SpaceContextEvent(
    string EventId,
    Guid SpaceId,
    string Kind,
    string Title,
    string? Payload,
    string? AuthorMemberId,
    DateTime TimestampUtc);
