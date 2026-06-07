namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public enum DelegationFleetPriority
{
    UserInitiated = 0,
    Scheduled = 1,
    Retry = 2
}

public sealed record BackgroundDelegationRequest(
    Guid RunId,
    string DelegationId,
    string Task,
    DelegationFleetPriority Priority = DelegationFleetPriority.UserInitiated,
    string? TenantUserId = null);

public sealed record BackgroundDelegationSnapshot(
    Guid RunId,
    string DelegationId,
    string Task,
    string QueueStatus,
    DelegationFleetPriority Priority,
    string? TenantUserId,
    DateTime EnqueuedAtUtc,
    DateTime? StartedAtUtc);

public sealed record BackgroundFleetListQuery(
    Guid? RunId = null,
    string? TenantUserId = null,
    bool ActiveOnly = true);

public sealed record BackgroundFleetSummary(
    int RunningCount,
    int QueuedCount,
    IReadOnlyList<BackgroundDelegationSnapshot> Items);
