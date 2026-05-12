using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Matches;
using Libr4.Matching.Domain.Profiles;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.Matching;

public sealed class HybridMatchingService : IMatchingService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorIndex _vectorIndex;
    private readonly IMatchRepository _matchRepo;
    private readonly ILogger<HybridMatchingService> _logger;

    public HybridMatchingService(
        IEmbeddingService embeddings,
        IVectorIndex vectorIndex,
        IMatchRepository matchRepo,
        ILogger<HybridMatchingService> logger)
    {
        _embeddings = embeddings;
        _vectorIndex = vectorIndex;
        _matchRepo = matchRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchResult>> FindMatchesForTaskAsync(
        Guid taskId,
        int topK = 20,
        CancellationToken ct = default)
    {
        var weights = await _matchRepo.GetCurrentWeightsAsync(ct);

        var taskEmbedding = await _embeddings.EmbedAsync($"task:{taskId}", ct);

        var candidates = await _vectorIndex.SearchFreelancersAsync(
            taskEmbedding, topK: topK * 3, minScore: 0.35f, ct: ct);

        if (!candidates.Any())
            return Array.Empty<MatchResult>();

        var taskProfile = new HybridScorer.TaskProfile
        {
            TaskId = taskId,
            Title = string.Empty,
            Description = string.Empty,
            RequiredSkills = Array.Empty<string>(),
            BudgetMin = 0,
            BudgetMax = int.MaxValue,
            DurationDays = 30,
            PostedAt = DateTimeOffset.UtcNow,
            Embedding = taskEmbedding.Select(x => (float)x).ToArray(),
        };

        var freelancerProfiles = candidates.Select(c => new HybridScorer.FreelancerProfile
        {
            FreelancerId = c.Id,
            Skills = c.Payload.TryGetValue("skills", out var s)
                ? s.Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            Interests = Array.Empty<string>(),
            AverageRating = c.Payload.TryGetValue("rating", out var r)
                ? double.TryParse(r, out var rv) ? rv : 0.0 : 0.0,
            CompletedTasks = c.Payload.TryGetValue("completed_tasks", out var ct2)
                ? int.TryParse(ct2, out var ctv) ? ctv : 0 : 0,
            HourlyRateMin = 0,
            HourlyRateMax = int.MaxValue,
            Embedding = Array.Empty<float>(),
        }).ToArray();

        var scores = HybridScorer.rankFreelancers(weights, taskProfile, freelancerProfiles);

        var results = scores
            .Take(topK)
            .Select(s => new MatchResult(
                FreelancerId: s.FreelancerId,
                TaskId: taskId,
                TotalScore: (float)s.TotalScore,
                KeywordScore: (float)s.KeywordScore,
                SemanticScore: (float)s.SemanticScore,
                MatchingSkills: s.MatchingSkills,
                Explanation: s.Explanation))
            .ToList();

        foreach (var r in results)
        {
            var match = Match.Create(
                r.TaskId, r.FreelancerId, r.TotalScore,
                r.KeywordScore, r.SemanticScore,
                r.MatchingSkills, r.Explanation);
            await _matchRepo.SaveAsync(match, ct);
        }

        _logger.LogInformation(
            "Matched task {TaskId}: {Count} candidates, top score {Top:F3}",
            taskId, results.Count, results.FirstOrDefault()?.TotalScore);

        return results;
    }

    public async Task<IReadOnlyList<MatchResult>> FindMatchesForFreelancerAsync(
        Guid freelancerId,
        int topK = 20,
        CancellationToken ct = default)
    {
        var weights = await _matchRepo.GetCurrentWeightsAsync(ct);
        var freelancerEmbedding = await _embeddings.EmbedAsync($"freelancer:{freelancerId}", ct);

        var candidates = await _vectorIndex.SearchTasksAsync(
            freelancerEmbedding, topK: topK, minScore: 0.35f, ct: ct);

        return candidates.Select(c => new MatchResult(
            FreelancerId: freelancerId,
            TaskId: c.Id,
            TotalScore: c.Score,
            KeywordScore: 0f,
            SemanticScore: c.Score,
            MatchingSkills: Array.Empty<string>(),
            Explanation: "Семантически близкая задача.")).ToList();
    }

    public async Task IndexFreelancerAsync(FreelancerMatchProfile profile, CancellationToken ct = default)
    {
        var text = $"{string.Join(", ", profile.Skills)} {string.Join(", ", profile.Interests)}";
        var embedding = await _embeddings.EmbedAsync(text, ct);

        await _vectorIndex.UpsertFreelancerAsync(profile.FreelancerId, embedding,
            new Dictionary<string, object>
            {
                ["skills"]          = string.Join(",", profile.Skills),
                ["rating"]          = profile.AverageRating.ToString("F2"),
                ["completed_tasks"] = profile.CompletedTasks.ToString(),
                ["rate_min"]        = profile.HourlyRateMin.ToString(),
                ["rate_max"]        = profile.HourlyRateMax.ToString(),
            }, ct);
    }

    public async Task IndexTaskAsync(TaskMatchProfile profile, CancellationToken ct = default)
    {
        var text = $"{profile.Title}. {profile.Description}. {string.Join(", ", profile.RequiredSkills)}";
        var embedding = await _embeddings.EmbedAsync(text, ct);

        await _vectorIndex.UpsertTaskAsync(profile.TaskId, embedding,
            new Dictionary<string, object>
            {
                ["title"]  = profile.Title,
                ["skills"] = string.Join(",", profile.RequiredSkills),
                ["budget_max"] = profile.BudgetMax.ToString(),
            }, ct);
    }

    public async Task RecordFeedbackAsync(
        Guid matchId,
        MatchFeedback feedback,
        CancellationToken ct = default)
    {
        var match = await _matchRepo.GetByIdAsync(matchId, ct);
        if (match is null) return;

        match.RecordFeedback(feedback);
        await _matchRepo.SaveAsync(match, ct);

        var currentWeights = await _matchRepo.GetCurrentWeightsAsync(ct);

        var fsMatchScore = new HybridScorer.MatchScore
        {
            FreelancerId = match.FreelancerId,
            TaskId = match.TaskId,
            TotalScore = match.TotalScore,
            KeywordScore = match.KeywordScore,
            SemanticScore = match.SemanticScore,
            ExperienceScore = 0.0,
            ReputationScore = 0.0,
            RecencyScore = 0.0,
            BudgetFitScore = 0.0,
            MatchingSkills = match.MatchingSkills.ToArray(),
            Explanation = match.Explanation,
        };

        var fsFeedback = feedback switch
        {
            MatchFeedback.Hired    => FeedbackModel.FeedbackSignal.Hired,
            MatchFeedback.Rejected => FeedbackModel.FeedbackSignal.Rejected,
            MatchFeedback.Applied  => FeedbackModel.FeedbackSignal.Applied,
            _                      => FeedbackModel.FeedbackSignal.Viewed,
        };

        var newWeights = FeedbackModel.updateWeights(
            currentWeights, fsMatchScore, fsFeedback, 0.01);

        await _matchRepo.SaveWeightsAsync(newWeights, ct);
    }
}
