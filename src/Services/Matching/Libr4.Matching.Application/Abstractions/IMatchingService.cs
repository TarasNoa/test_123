using Libr4.Matching.Domain.Matches;
using Libr4.Matching.Domain.Profiles;

namespace Libr4.Matching.Application.Abstractions;

public interface IMatchingService
{
    Task<IReadOnlyList<MatchResult>> FindMatchesForTaskAsync(
        Guid taskId,
        int topK = 20,
        CancellationToken ct = default);

    Task<IReadOnlyList<MatchResult>> FindMatchesForFreelancerAsync(
        Guid freelancerId,
        int topK = 20,
        CancellationToken ct = default);

    Task IndexFreelancerAsync(FreelancerMatchProfile profile, CancellationToken ct = default);
    Task IndexTaskAsync(TaskMatchProfile profile, CancellationToken ct = default);

    Task RecordFeedbackAsync(
        Guid matchId,
        MatchFeedback feedback,
        CancellationToken ct = default);
}
