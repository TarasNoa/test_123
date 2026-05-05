namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record McpExecutionAuditEntry(
    string ToolName,
    string ServerName,
    McpExecutionLaneKind Lane,
    McpToolRiskLevel RiskLevel,
    string ArgumentsSha256,
    DateTime StartedAtUtc,
    long DurationMs,
    string Outcome,
    string? Detail);
