namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;

public sealed record AgentSessionRecord(
    string SessionId,
    Guid? RunId,
    string? SubagentId,
    string? Model,
    string Status,
    DateTime CreatedAtUtc,
    DateTime LastStepAtUtc,
    int TokenBudget,
    double CostUsd,
    string PermissionMode,
    int CurrentStepNumber);

public sealed record AgentMessageRecord(
    long Id,
    string SessionId,
    string Role,
    string Content,
    string? ToolCallsJson,
    int StepNumber,
    DateTime TimestampUtc);

public sealed record AgentToolCallRecord(
    long Id,
    string SessionId,
    string ToolName,
    string InputJson,
    string? OutputJson,
    bool Success,
    long DurationMs,
    DateTime StartedAtUtc);

public sealed record AgentCheckpointRecord(
    string CheckpointId,
    string SessionId,
    int StepNumber,
    string MessagesJson,
    string FileHashesJson,
    DateTime CreatedAtUtc);
