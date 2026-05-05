namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Audit entry for skill invocation with provenance tracking.
/// Captures which subagent/skill profile was chosen and why.
/// </summary>
public sealed record SkillInvocationAuditEntry(
    string SkillId,
    string Version,
    string Stage,
    string SafetyLabel,
    DateTime StartedAtUtc,
    long DurationMs,
    string Outcome,
    string? Detail,
    // Provenance fields for schema-driven skill contracts
    string SelectionReason,
    string? ModelProfile,
    string? ToolProfile,
    string? RuntimeProfile);
