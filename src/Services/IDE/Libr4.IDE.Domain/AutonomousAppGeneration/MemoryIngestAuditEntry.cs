namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record MemoryIngestAuditEntry(
    Guid RunId,
    string Stage,
    MemoryKind Kind,
    string Key,
    string Summary,
    int TokenEstimate,
    DateTime StoredAtUtc);
