using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed record PostRunLesson(
    string Key,
    string Summary,
    MemoryKind Kind,
    double Confidence = 1.0);

public sealed record PostRunExtractionRequest(
    Guid RunId,
    GenerationStatus Status,
    string RequestFingerprint,
    string? FailureReason,
    string? ApplicationName,
    string? StackPattern,
    IReadOnlyList<string> RolloutLines,
    IReadOnlyList<ErrorReport> Errors,
    int IterationCount);

public sealed record PostRunExtractionResult(
    Guid RunId,
    string Status,
    IReadOnlyList<PostRunLesson> Lessons,
    string Source);
