using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed record HermesMemoryEntry(
    Guid Id,
    Guid RunId,
    string? UserId,
    string RequestFingerprint,
    MemoryKind Kind,
    string Stage,
    string Key,
    string Summary,
    string? PayloadJson,
    int Tokens,
    double Score,
    DateTime CreatedAtUtc);

public sealed record HermesMemoryQuery(
    string RequestFingerprint,
    string? Keyword = null,
    int TopK = 16,
    MemoryKind[]? Kinds = null,
    string? UserId = null);

public sealed record HermesMemoryRetrievalResult(
    HermesMemoryEntry Entry,
    string RetrievalReason,
    double RelevanceScore);
