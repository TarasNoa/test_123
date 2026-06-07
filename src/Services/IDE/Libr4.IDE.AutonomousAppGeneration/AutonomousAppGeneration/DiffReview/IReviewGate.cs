namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IReviewGate
{
    bool RequireHumanReview { get; }

    Task<bool> IsApprovedAsync(Guid runId, CancellationToken ct = default);

    Task<RunReviewStatus> GetStatusAsync(Guid runId, CancellationToken ct = default);
}
