namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public static class FleetCiStatus
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Success = "success";
    public const string Failure = "failure";
}

public sealed record RunShipState(
    Guid RunId,
    int? PullRequestNumber,
    string? PullRequestUrl,
    string? HeadBranch,
    string CiStatus,
    string? CiLogsUrl,
    DateTime UpdatedAtUtc,
    AgentFleetStatus? ManualStatusOverride = null);

public sealed record GitHubCiWebhookPayload(
    string EventType,
    string? Action,
    string? HeadBranch,
    string? Conclusion,
    string? HtmlUrl,
    Guid? RunId = null);
