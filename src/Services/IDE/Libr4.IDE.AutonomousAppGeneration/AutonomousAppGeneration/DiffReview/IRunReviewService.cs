namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IRunReviewService
{
    Task<RunReviewStatusResponse> GetStatusAsync(Guid runId, CancellationToken ct = default);

    Task<RunReviewStatusResponse> SubmitAsync(
        Guid runId,
        ReviewSubmissionRequest request,
        CancellationToken ct = default);
}
