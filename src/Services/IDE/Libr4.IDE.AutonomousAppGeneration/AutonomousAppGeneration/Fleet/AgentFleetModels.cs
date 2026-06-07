namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public enum AgentFleetStatus
{
    Queued,
    Planning,
    Generating,
    Verifying,
    Repairing,
    WaitingForApproval,
    WaitingForCi,
    PrReady,
    HandoffPending,
    HandoffComplete,
    Completed,
    Failed,
    Cancelled
}

public sealed record AgentFleetEntry(
    Guid RunId,
    string Title,
    string? SpaceId,
    AgentFleetStatus Status,
    string Stage,
    int AgentCount,
    DateTime StartedAtUtc,
    DateTime LastActivityAtUtc,
    double CostUsd,
    string? ModelProfile,
    string? VerifyStatus,
    string? Stack,
    bool Pinned,
    bool Archived,
    string? FailureReason,
    string? BackendKind = null,
    string? BackendFallbackFrom = null,
    string? PrUrl = null,
    int? PrNumber = null,
    string? CiStatus = null,
    string? CiLogsUrl = null,
    int PlaybookHits = 0,
    int PlaybookAttempts = 0,
    int QualityScore = 0);

public sealed record AgentFleetSummary(
    Guid RunId,
    string Title,
    AgentFleetStatus Status,
    string Stage,
    int AgentCount,
    DateTime LastActivityAtUtc,
    bool Pinned,
    bool Archived,
    string? BackendKind = null,
    string? BackendFallbackFrom = null,
    string? PrUrl = null,
    int? PrNumber = null,
    string? CiStatus = null,
    string? CiLogsUrl = null,
    int PlaybookHits = 0,
    int PlaybookAttempts = 0,
    int QualityScore = 0);

public sealed record AgentFleetRunDetail(
    AgentFleetEntry Entry,
    int SubagentCount,
    int DelegationCount,
    int EvidenceCount,
    string? FlowName,
    string? CurrentFlowNodeId,
    string? LastError);

public sealed record AgentFleetListQuery(
    AgentFleetStatus? Status = null,
    string? SpaceId = null,
    string? Stack = null,
    string? Search = null,
    bool IncludeArchived = false,
    int Limit = 100,
    string? SortBy = null);

public sealed record AgentFleetPatchRequest(
    string? Title = null,
    bool? Pinned = null,
    bool? Archived = null,
    string? Actor = null,
    AgentFleetStatus? StatusOverride = null);

public sealed record AgentFleetBulkArchiveRequest(
    IReadOnlyList<Guid>? RunIds = null,
    int OlderThanDays = 7,
    string? Actor = null);

public sealed record AgentFleetStatusEvent(
    Guid RunId,
    AgentFleetStatus Status,
    string Stage,
    DateTime TimestampUtc);
