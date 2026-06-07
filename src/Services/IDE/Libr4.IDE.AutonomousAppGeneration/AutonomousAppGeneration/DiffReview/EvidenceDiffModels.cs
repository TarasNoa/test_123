namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed record DiffEvidenceItem(
    string Source,
    string Kind,
    string FileName,
    string DownloadUrl,
    string? ThumbnailUrl,
    int? StepNumber,
    string? ToolName,
    bool StepMatched,
    long SizeBytes,
    DateTime LastModifiedUtc);

public sealed record DiffEvidenceOverlay(
    string Kind,
    string Reason,
    string? Category);

public sealed record FileDiffEvidenceResponse(
    Guid RunId,
    string Path,
    int? CorrelatedStepNumber,
    IReadOnlyList<DiffEvidenceItem> Items,
    IReadOnlyList<DiffEvidenceOverlay> Overlays);

public sealed record DiffPathOverlay(
    string Path,
    IReadOnlyList<string> OverlayKinds,
    IReadOnlyList<string> Reasons);

public sealed record DiffEvidenceOverlayIndex(
    Guid RunId,
    IReadOnlyList<DiffPathOverlay> Paths);
