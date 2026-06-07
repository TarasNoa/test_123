using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class ReviewGate : IReviewGate
{
    private readonly IRunReviewService _reviews;
    private readonly HumanReviewOptions _options;

    public ReviewGate(
        IRunReviewService reviews,
        IOptions<HumanReviewOptions> options)
    {
        _reviews = reviews;
        _options = options.Value;
    }

    public bool RequireHumanReview => _options.RequireHumanReview;

    public async Task<bool> IsApprovedAsync(Guid runId, CancellationToken ct = default)
    {
        if (!_options.RequireHumanReview)
            return true;

        var status = await _reviews.GetStatusAsync(runId, ct).ConfigureAwait(false);
        return status.Status == RunReviewStatus.Approved
               || status.Status == RunReviewStatus.NotRequired;
    }

    public async Task<RunReviewStatus> GetStatusAsync(Guid runId, CancellationToken ct = default)
    {
        var status = await _reviews.GetStatusAsync(runId, ct).ConfigureAwait(false);
        return status.Status;
    }
}
