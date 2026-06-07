namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public enum ReviewDecision
{
    Approve,
    Reject,
    RequestRepair,
    ApproveWithNotes
}

public enum RunReviewStatus
{
    NotRequired,
    Pending,
    Partial,
    Approved,
    Rejected,
    RepairRequested
}

public sealed record ReviewSubmissionRequest(
    ReviewDecision Decision,
    IReadOnlyList<string> Paths,
    string? Notes = null,
    string? ReviewerId = null);

public sealed record FileReviewState(
    string Path,
    ReviewDecision Decision,
    string? Notes,
    string? ReviewerId,
    DateTime DecidedAtUtc);

public sealed record RunReviewStatusResponse(
    Guid RunId,
    RunReviewStatus Status,
    bool RequireHumanReview,
    int TotalFiles,
    int DecidedFiles,
    int ApprovedFiles,
    int RejectedFiles,
    int RepairRequestedFiles,
    IReadOnlyList<FileReviewState> Files,
    IReadOnlyList<string> PendingPaths);

public sealed record ReviewDecisionAuditEntry(
    Guid RunId,
    string Path,
    ReviewDecision Decision,
    string? Notes,
    string? ReviewerId,
    DateTime TimestampUtc);
