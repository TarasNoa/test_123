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
    private readonly ITaskDataClient _taskClient;
    private readonly IUserSkillsClient _skillsClient;
    private readonly ILogger<HybridMatchingService> _logger;

    public HybridMatchingService(
        IEmbeddingService embeddings,
        IVectorIndex vectorIndex,
        IMatchRepository matchRepo,
        ITaskDataClient taskClient,
        IUserSkillsClient skillsClient,
        ILogger<HybridMatchingService> logger)
    {
        _embeddings = embeddings;
        _vectorIndex = vectorIndex;
        _matchRepo = matchRepo;
        _taskClient = taskClient;
        _skillsClient = skillsClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchResult>> FindMatchesForTaskAsync(
        Guid taskId,
        int topK = 20,
        CancellationToken ct = default)
    {
        var weights = await _matchRepo.GetCurrentWeightsAsync(ct);

        var taskData = await _taskClient.GetTaskAsync(taskId, ct);
        var taskText = taskData != null
            ? $"{taskData.Title} {taskData.Description} {taskData.Category}"
            : $"task:{taskId}";
        var taskEmbedding = await _embeddings.EmbedAsync(taskText, ct);

        var candidates = await _vectorIndex.SearchFreelancersAsync(
            taskEmbedding, topK: topK * 3, minScore: 0.35f, ct: ct);

        if (!candidates.Any())
            return Array.Empty<MatchResult>();

        var taskProfile = new HybridScorer.TaskProfile
        {
            TaskId = taskId,
            Title = taskData?.Title ?? string.Empty,
            Description = taskData?.Description ?? string.Empty,
            RequiredSkills = taskData?.Category is not null
                ? new[] { taskData.Category }
                : Array.Empty<string>(),
            BudgetMin = 0,
            BudgetMax = (int)(taskData?.Budget ?? int.MaxValue),
            DurationDays = 30,
            PostedAt = taskData?.CreatedAt ?? DateTimeOffset.UtcNow,
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
        FreelancerSkillSummary? skillSummary = null;
        try
        {
            skillSummary = await _skillsClient.GetFreelancerSkillsAsync(freelancerId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch skills for freelancer {Id}, using generic search", freelancerId);
        }

        string queryText;
        if (skillSummary is { Skills.Count: > 0 })
        {
            var profile = new FreelancerMatchProfile
            {
                FreelancerId = freelancerId,
                OverallLevel = skillSummary.OverallLevel,
                PrimaryExpertise = skillSummary.PrimaryExpertise,
                SecondaryExpertise = skillSummary.SecondaryExpertise,
                SkillScores = skillSummary.Skills.ToDictionary(s => s.Name, s => s.Score),
                SkillLevels = skillSummary.Skills.ToDictionary(s => s.Name, s => s.Level),
                SkillExperienceYears = skillSummary.Skills.ToDictionary(s => s.Name, s => s.ExperienceYears),
            };
            queryText = profile.BuildEmbeddingText();
        }
        else
        {
            queryText = "software development programming task project";
        }

        _logger.LogInformation(
            "Finding tasks for freelancer {Id}. Query: '{Query}'",
            freelancerId, queryText.Length > 80 ? queryText[..80] + "..." : queryText);

        var queryEmbedding = await _embeddings.EmbedAsync(queryText, ct);
        var candidates = await _vectorIndex.SearchTasksAsync(
            queryEmbedding, topK: topK * 2, minScore: 0.30f, ct: ct);

        if (!candidates.Any()) return Array.Empty<MatchResult>();

        var results = candidates
            .Take(topK)
            .Select(c =>
            {
                var taskSkills = c.Payload.TryGetValue("skills", out var ts)
                    ? ts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray()
                    : Array.Empty<string>();

                var matchingSkills = skillSummary is not null
                    ? taskSkills.Intersect(
                        skillSummary.Skills.Select(s => s.Name),
                        StringComparer.OrdinalIgnoreCase).ToArray()
                    : Array.Empty<string>();

                var explanation = BuildExplanation(skillSummary, matchingSkills, c.Score);

                return new MatchResult(
                    FreelancerId: freelancerId,
                    TaskId: c.Id,
                    TotalScore: c.Score,
                    KeywordScore: matchingSkills.Any() ? 0.5f * matchingSkills.Length / Math.Max(taskSkills.Length, 1) : 0f,
                    SemanticScore: c.Score,
                    MatchingSkills: matchingSkills,
                    Explanation: explanation);
            }).ToList();

        return results;
    }

    public async Task IndexFreelancerAsync(FreelancerMatchProfile profile, CancellationToken ct = default)
    {
        var text = profile.BuildEmbeddingText();
        var embedding = await _embeddings.EmbedAsync(text, ct);

        var payload = new Dictionary<string, object>
        {
            ["skills"]          = string.Join(",", profile.Skills),
            ["rating"]          = profile.AverageRating.ToString("F2"),
            ["completed_tasks"] = profile.CompletedTasks.ToString(),
            ["rate_min"]        = profile.HourlyRateMin.ToString(),
            ["rate_max"]        = profile.HourlyRateMax.ToString(),
            ["overall_level"]   = profile.OverallLevel,
            ["overall_score"]   = profile.OverallScore.ToString("F1"),
            ["primary_expertise"] = profile.PrimaryExpertise,
        };

        if (profile.SkillScores.Any())
        {
            var topSkills = profile.SkillScores
                .OrderByDescending(x => x.Value)
                .Take(10)
                .Select(x => $"{x.Key}:{x.Value:F0}");
            payload["skill_scores"] = string.Join(",", topSkills);
        }

        await _vectorIndex.UpsertFreelancerAsync(profile.FreelancerId, embedding, payload, ct);
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

    private static string BuildExplanation(
        FreelancerSkillSummary? summary,
        string[] matchingSkills,
        float score)
    {
        if (summary is null)
            return $"Semantically similar task (score: {score:F2}).";

        if (!matchingSkills.Any())
            return $"Task matches your specialization '{summary.PrimaryExpertise}' (score: {score:F2}).";

        return $"Matching skills: {string.Join(", ", matchingSkills)}. " +
               $"Suitable for {summary.OverallLevel} level (score: {score:F2}).";
    }
}
