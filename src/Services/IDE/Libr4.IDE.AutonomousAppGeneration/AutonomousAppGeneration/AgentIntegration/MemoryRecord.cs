using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed record MemoryRecord(
    Guid RunId,
    string RequestFingerprint,
    string Stage,
    MemoryKind Kind,
    string Key,
    string Summary,
    string? PayloadJson,
    int TokenEstimate,
    DateTime CreatedAtUtc);

public sealed record MemoryQuery(
    string RequestFingerprint,
    string? Keyword,
    int TopK,
    MemoryKind[]? Kinds = null);

/// <summary>
/// Memory retrieval result with explainable provenance.
/// Shows why a specific memory item was selected during retrieval.
/// </summary>
public sealed record MemoryRetrievalResult(
    MemoryRecord Record,
    string RetrievalReason,
    double RelevanceScore);
