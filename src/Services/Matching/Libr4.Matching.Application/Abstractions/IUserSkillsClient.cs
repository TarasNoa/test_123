namespace Libr4.Matching.Application.Abstractions;

public interface IUserSkillsClient
{
    Task<FreelancerSkillSummary?> GetFreelancerSkillsAsync(Guid userId, CancellationToken ct = default);
}

public sealed record FreelancerSkillSummary(
    Guid UserId,
    string OverallLevel,
    float OverallScore,
    string PrimaryExpertise,
    List<string> SecondaryExpertise,
    List<FreelancerSkillItem> Skills);

public sealed record FreelancerSkillItem(
    string Name,
    float Score,
    string Level,
    int ExperienceYears);
