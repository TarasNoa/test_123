namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record CheckpointAuditEntry(
    Guid RunId,
    string CheckpointId,
    string Label,
    string Action,
    int FileCount,
    int ChangedFiles,
    string? Detail,
    DateTime CreatedAtUtc);
