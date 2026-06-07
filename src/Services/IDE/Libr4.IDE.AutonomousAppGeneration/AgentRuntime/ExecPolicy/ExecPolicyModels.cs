namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;

public enum ExecPolicyDecision
{
    Allow,
    Prompt,
    Forbid
}

public sealed record ExecPolicyRule(
    string Action,
    string Pattern,
    ExecPolicyDecision Decision);

public sealed record ExecPolicyEvaluation(
    ExecPolicyDecision Decision,
    string? MatchedRule,
    string? Reason);

public sealed record ExecPolicyAuditEntry(
    string Action,
    string CommandOrTarget,
    ExecPolicyDecision Decision,
    string? MatchedRule,
    Guid? RunId,
    DateTime TimestampUtc);
