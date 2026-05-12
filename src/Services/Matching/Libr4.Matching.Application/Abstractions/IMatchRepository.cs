using Libr4.Matching.Domain.Matches;

namespace Libr4.Matching.Application.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Match>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task SaveAsync(Match match, CancellationToken ct = default);
    Task<HybridScorer.ScoringWeights> GetCurrentWeightsAsync(CancellationToken ct = default);
    Task SaveWeightsAsync(HybridScorer.ScoringWeights weights, CancellationToken ct = default);
}
