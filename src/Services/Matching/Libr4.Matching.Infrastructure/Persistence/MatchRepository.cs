using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Matching.Infrastructure.Persistence;

public sealed class MatchRepository : IMatchRepository
{
    private readonly MatchingDbContext _db;

    public MatchRepository(MatchingDbContext db)
    {
        _db = db;
    }

    public Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Matches.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<List<Match>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
        => _db.Matches.Where(m => m.TaskId == taskId).ToListAsync(ct);

    public async Task SaveAsync(Match match, CancellationToken ct = default)
    {
        var existing = await _db.Matches.FindAsync(new object[] { match.Id }, ct);
        if (existing is null)
            _db.Matches.Add(match);
        else
            _db.Entry(existing).CurrentValues.SetValues(match);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HybridScorer.ScoringWeights> GetCurrentWeightsAsync(CancellationToken ct = default)
    {
        var entity = await _db.ScoringWeights
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            return HybridScorer.defaultWeights;

        return new HybridScorer.ScoringWeights
        {
            KeywordSkillWeight = entity.KeywordSkillWeight,
            SemanticWeight     = entity.SemanticWeight,
            ExperienceWeight   = entity.ExperienceWeight,
            ReputationWeight   = entity.ReputationWeight,
            RecencyWeight      = entity.RecencyWeight,
            BudgetFitWeight    = entity.BudgetFitWeight,
        };
    }

    public async Task SaveWeightsAsync(HybridScorer.ScoringWeights weights, CancellationToken ct = default)
    {
        await _db.ScoringWeights
            .Where(w => w.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsActive, false), ct);

        _db.ScoringWeights.Add(new ScoringWeightsEntity
        {
            IsActive           = true,
            KeywordSkillWeight = weights.KeywordSkillWeight,
            SemanticWeight     = weights.SemanticWeight,
            ExperienceWeight   = weights.ExperienceWeight,
            ReputationWeight   = weights.ReputationWeight,
            RecencyWeight      = weights.RecencyWeight,
            BudgetFitWeight    = weights.BudgetFitWeight,
            UpdatedAt          = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
    }

    public Task<FreelancerStats?> GetFreelancerStatsAsync(Guid freelancerId, CancellationToken ct = default)
        => Task.FromResult<FreelancerStats?>(null);
}
