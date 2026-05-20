namespace Libr4.Shared.Contracts.IntegrationEvents.Auth;

public sealed record UserLoggedInIntegrationEvent(
    Guid UserId,
    string Email,
    string? IpAddress,
    DateTimeOffset OccurredOn);

public sealed record EmailConfirmationRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTimeOffset OccurredOn);

public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTimeOffset OccurredOn);

public sealed record SkillAssessmentCompletedIntegrationEvent(
    Guid UserId,
    string OverallLevel,
    float OverallScore,
    string PrimaryExpertise,
    List<string> SecondaryExpertise,
    List<AssessedSkillDto> Skills,
    List<string> Recommendations,
    DateTimeOffset OccurredOn);

public sealed record AssessedSkillDto(
    string Name,
    float Score,
    string Level,
    int ExperienceYears,
    List<string> Contexts);
