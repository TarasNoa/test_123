namespace Libr4.AI.Application.CVAnalysis;

public sealed record CVAnalysisRequest(
    Guid UserId,
    string? CvText,
    string? LinkedInUrl,
    LinkedInProfileData? LinkedInData);

public sealed record LinkedInProfileData(
    string Headline,
    string Summary,
    List<LinkedInExperience> Experience,
    List<LinkedInEducation> Education,
    List<string> Skills);

public sealed record LinkedInExperience(
    string Title,
    string Company,
    int DurationYears,
    List<string> Skills);

public sealed record LinkedInEducation(
    string Degree,
    string School,
    int Year);

public sealed record CVAnalysisResult(
    Guid UserId,
    List<ExtractedSkill> Skills,
    string OverallLevel,
    float OverallScore,
    string PrimaryExpertise,
    List<string> SecondaryExpertise,
    List<string> Recommendations,
    DateTimeOffset AnalyzedAt);

public sealed record ExtractedSkill(
    string Name,
    float Score,
    string Level,
    string Source,
    int ExperienceYears,
    List<string> Contexts);
