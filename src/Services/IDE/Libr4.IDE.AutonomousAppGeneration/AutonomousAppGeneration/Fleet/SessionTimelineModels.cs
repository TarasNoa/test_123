namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public enum SessionTimelineKind
{
    Phase,
    ToolCall,
    SubagentSpawn,
    SubagentComplete,
    DelegationStart,
    DelegationComplete,
    VerifyAttempt,
    FlowNode,
    StepStart,
    StepFinish,
    Error,
    Permission,
    ExecPolicyConsent
}

public sealed record SessionTimelineEvent(
    SessionTimelineKind Kind,
    DateTime TimestampUtc,
    string Title,
    string? Detail,
    bool? Success,
    int? StepNumber,
    string? ActorId);

public sealed record SessionTimelineResponse(
    Guid RunId,
    IReadOnlyList<SessionTimelineEvent> Events);
