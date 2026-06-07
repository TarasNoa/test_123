namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public sealed record DelegationRecord(
    string Id,
    Guid RunId,
    string Task,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? OutputPreview,
    string? Error,
    bool NotificationPending);

public sealed class DelegationNotification
{
    public required string DelegationId { get; init; }
    public required string Summary { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public string? OutputRelativePath { get; init; }
}
