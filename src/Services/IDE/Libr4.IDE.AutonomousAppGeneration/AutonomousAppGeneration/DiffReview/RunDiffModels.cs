namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public enum RunDiffChangeKind
{
    Add,
    Modify,
    Delete
}

public sealed record RunDiffHunk(
    int StartLine,
    int EndLine,
    string? UnifiedDiff,
    string? Snippet,
    string ProvenanceId);

public sealed record RunFileDiff(
    string Path,
    string Language,
    RunDiffChangeKind ChangeKind,
    int StepNumber,
    string ToolName,
    string? AgentRole,
    IReadOnlyList<RunDiffHunk> Hunks,
    DateTime LastChangedUtc,
    string ProvenanceId);

public sealed record RunDiffListResponse(
    Guid RunId,
    int Total,
    IReadOnlyList<RunFileDiffSummary> Items);

public sealed record RunFileDiffSummary(
    string Path,
    string Language,
    RunDiffChangeKind ChangeKind,
    int StepNumber,
    string ToolName,
    int HunkCount,
    DateTime LastChangedUtc,
    string ProvenanceId);

public sealed record RunFileDiffDetail(
    Guid RunId,
    string Path,
    string Language,
    RunDiffChangeKind ChangeKind,
    IReadOnlyList<RunDiffHunk> Hunks,
    string? UnifiedDiff,
    IReadOnlyList<RunDiffProvenance> Provenance);

public sealed record RunDiffProvenance(
    string Id,
    int StepNumber,
    string ToolName,
    bool Success,
    long? DurationMs,
    DateTime TimestampUtc);

public sealed record RunDiffQuery(
    string? PathFilter = null,
    int? StepNumber = null,
    int Offset = 0,
    int Limit = 50,
    string? CheckpointTag = null);

public sealed record RunDiffCheckpointSummary(
    string Tag,
    int AttemptNumber,
    DateTime TaggedAtUtc,
    int FileCount);

public sealed record RunDiffCheckpointFile(
    string Path,
    RunDiffChangeKind ChangeKind,
    string Language,
    string? UnifiedDiff);

public sealed record RunDiffCheckpointSnapshot(
    Guid RunId,
    string Tag,
    int AttemptNumber,
    DateTime TaggedAtUtc,
    IReadOnlyList<RunDiffCheckpointFile> Files);

public sealed record RunDiffCheckpointListResponse(
    Guid RunId,
    IReadOnlyList<RunDiffCheckpointSummary> Checkpoints);
