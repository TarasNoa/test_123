using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Profiles;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Application.IntegrationEvents.Consumers;

// Ensure usings are available

public sealed class SkillAssessmentCompletedConsumer : IConsumer<SkillAssessmentCompletedIntegrationEvent>
{
    private readonly IMatchingService _matchingService;
    private readonly IMatchRepository _matchRepo;
    private readonly ILogger<SkillAssessmentCompletedConsumer> _logger;

    public SkillAssessmentCompletedConsumer(
        IMatchingService matchingService,
        IMatchRepository matchRepo,
        ILogger<SkillAssessmentCompletedConsumer> logger)
    {
        _matchingService = matchingService;
        _matchRepo = matchRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SkillAssessmentCompletedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "Reindexing freelancer {UserId} after skill assessment. " +
            "Level: {Level}, Score: {Score:F1}, Skills: {SkillCount}",
            msg.UserId, msg.OverallLevel, msg.OverallScore, msg.Skills.Count);

        var profile = new FreelancerMatchProfile
        {
            FreelancerId = msg.UserId,
            OverallLevel = msg.OverallLevel,
            OverallScore = msg.OverallScore,
            PrimaryExpertise = msg.PrimaryExpertise,
            SecondaryExpertise = msg.SecondaryExpertise,
            Skills = msg.Skills.Select(s => s.Name).ToList(),
            SkillScores = msg.Skills.ToDictionary(s => s.Name, s => s.Score),
            SkillLevels = msg.Skills.ToDictionary(s => s.Name, s => s.Level),
            SkillExperienceYears = msg.Skills.ToDictionary(s => s.Name, s => s.ExperienceYears),
            AverageRating = 0,
            CompletedTasks = 0,
            IndexedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var existingStats = await _matchRepo.GetFreelancerStatsAsync(msg.UserId, ct);
            if (existingStats is not null)
            {
                profile.AverageRating = existingStats.AverageRating;
                profile.CompletedTasks = existingStats.CompletedTasks;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch existing stats for {UserId}", msg.UserId);
        }

        await _matchingService.IndexFreelancerAsync(profile, ct);

        _logger.LogInformation(
            "Freelancer {UserId} reindexed. Primary: {Primary}, Embedding text length: {Len}",
            msg.UserId, msg.PrimaryExpertise, profile.BuildEmbeddingText().Length);
    }
}
