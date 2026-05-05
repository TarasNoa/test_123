namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Audit entry for memory retrieval with explainable provenance.
/// Captures which memory items were retrieved and why.
/// </summary>
public sealed record MemoryRetrievalAuditEntry(
    Guid RunId,
    string Stage,
    MemoryKind Kind,
    string Key,
    string Summary,
    string RetrievalReason,
    double RelevanceScore,
    DateTime RetrievedAtUtc);
