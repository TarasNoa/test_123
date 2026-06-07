namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;

public sealed record ObscuraExecPolicyRule(
    string Kind,
    string Pattern,
    ExecPolicyDecision Decision);

public sealed record ObscuraExecPolicyEvaluation(
    ExecPolicyDecision Decision,
    string? MatchedRule,
    string? Reason);

public sealed record ObscuraExecPolicyAuditEntry(
    string ToolName,
    string? Target,
    ExecPolicyDecision Decision,
    string? MatchedRule,
    Guid? RunId,
    DateTime TimestampUtc);
