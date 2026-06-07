namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IRunReviewStore
{
    Task AppendAsync(ReviewDecisionAuditEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<ReviewDecisionAuditEntry>> LoadAsync(Guid runId, CancellationToken ct = default);

    string GetDecisionsPath(Guid runId);
}
