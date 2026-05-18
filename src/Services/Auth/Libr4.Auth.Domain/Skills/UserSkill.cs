namespace Libr4.Auth.Domain.Skills;

public sealed class UserSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Score { get; set; }  // 0-100 scale
    public string Level { get; set; } = "Intermediate";  // Beginner, Intermediate, Advanced, Expert
    public string Source { get; set; } = "cv";  // cv, linkedin, combined
    public int ExperienceYears { get; set; }
    public List<string> Contexts { get; set; } = new();
    public string? AssessmentReason { get; set; }  // AI explanation for the score
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SkillAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string OverallLevel { get; set; } = "Not assessed";
    public float OverallScore { get; set; }
    public string PrimaryExpertise { get; set; } = "Unknown";
    public List<string> SecondaryExpertise { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
}
