using System.Net.Http.Json;
using Libr4.Matching.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.Clients;

public sealed class HttpUserSkillsClient : IUserSkillsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpUserSkillsClient> _logger;

    public HttpUserSkillsClient(HttpClient http, ILogger<HttpUserSkillsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<FreelancerSkillSummary?> GetFreelancerSkillsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<AuthSkillsResponse>(
                $"/api/v1/users/skills/{userId}", ct);

            if (response is null || response.Skills.Count == 0) return null;

            return new FreelancerSkillSummary(
                UserId: userId,
                OverallLevel: response.OverallLevel,
                OverallScore: response.OverallScore,
                PrimaryExpertise: response.PrimaryExpertise,
                SecondaryExpertise: response.SecondaryExpertise,
                Skills: response.Skills.Select(s => new FreelancerSkillItem(
                    Name: s.Name,
                    Score: s.Score,
                    Level: s.Level,
                    ExperienceYears: s.ExperienceYears)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch skills for user {UserId}", userId);
            return null;
        }
    }

    private sealed record AuthSkillsResponse(
        Guid UserId,
        List<AuthSkillDto> Skills,
        string OverallLevel,
        float OverallScore,
        string PrimaryExpertise,
        List<string> SecondaryExpertise,
        List<string> Recommendations,
        DateTimeOffset? LastAssessedAt);

    private sealed record AuthSkillDto(
        Guid Id,
        string Name,
        float Score,
        string Level,
        string Source,
        int ExperienceYears,
        List<string> Contexts,
        string? AssessmentReason,
        DateTimeOffset AssessedAt);
}
