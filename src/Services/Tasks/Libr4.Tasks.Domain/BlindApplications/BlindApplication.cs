using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.BlindApplications;

public sealed class BlindApplication : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid ApplicantId { get; private set; }
    public string AnonymousId { get; private set; } = "";
    public string ProposalText { get; private set; } = "";
    public string? CoverLetter { get; private set; }
    public List<string> PortfolioLinks { get; private set; } = new();
    public decimal? HourlyRate { get; private set; }
    public decimal? FixedPrice { get; private set; }
    public int? EstimatedHours { get; private set; }
    public int? EstimatedDays { get; private set; }
    public string? Availability { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public Dictionary<string, object> AnonymizedProfile { get; private set; } = new();
    public List<string> SkillTags { get; private set; } = new();
    public string ExperienceLevel { get; private set; } = "intermediate";
    public double QualityScore { get; private set; }
    public double BiasScore { get; private set; }
    public double AiMatchScore { get; private set; }
    public BlindApplicationStatus Status { get; private set; }
    public string? ClientNotes { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? RevealedAt { get; private set; }

    private BlindApplication() { }

    public static BlindApplication Create(
        Guid taskId,
        Guid applicantId,
        string anonymousId,
        string proposalText,
        string? coverLetter,
        List<string>? portfolioLinks,
        decimal? hourlyRate,
        decimal? fixedPrice,
        int? estimatedHours,
        int? estimatedDays,
        string? availability,
        DateTimeOffset? startDate,
        Dictionary<string, object>? anonymizedProfile,
        List<string>? skillTags,
        string experienceLevel,
        double qualityScore,
        double biasScore,
        double aiMatchScore,
        DateTimeOffset now)
    {
        return new BlindApplication
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ApplicantId = applicantId,
            AnonymousId = anonymousId.Trim(),
            ProposalText = proposalText.Trim(),
            CoverLetter = coverLetter?.Trim(),
            PortfolioLinks = portfolioLinks ?? new(),
            HourlyRate = hourlyRate,
            FixedPrice = fixedPrice,
            EstimatedHours = estimatedHours,
            EstimatedDays = estimatedDays,
            Availability = availability?.Trim(),
            StartDate = startDate,
            AnonymizedProfile = anonymizedProfile ?? new(),
            SkillTags = skillTags ?? new(),
            ExperienceLevel = experienceLevel.Trim(),
            QualityScore = qualityScore,
            BiasScore = biasScore,
            AiMatchScore = aiMatchScore,
            Status = BlindApplicationStatus.Submitted,
            SubmittedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(BlindApplicationStatus status, string? clientNotes, DateTimeOffset now)
    {
        Status = status;
        ClientNotes = clientNotes?.Trim();
        UpdatedAt = now;
    }

    public void Shortlist(string? clientNotes, DateTimeOffset now)
    {
        Status = BlindApplicationStatus.Shortlisted;
        ClientNotes = clientNotes?.Trim();
        UpdatedAt = now;
    }

    public void Reject(string? clientNotes, DateTimeOffset now)
    {
        Status = BlindApplicationStatus.Rejected;
        ClientNotes = clientNotes?.Trim();
        UpdatedAt = now;
    }

    public void Accept(string? clientNotes, DateTimeOffset now)
    {
        Status = BlindApplicationStatus.Accepted;
        ClientNotes = clientNotes?.Trim();
        UpdatedAt = now;
    }

    public void Reveal(DateTimeOffset now)
    {
        RevealedAt = now;
        UpdatedAt = now;
    }

    public void UpdateScores(double qualityScore, double biasScore, double aiMatchScore, DateTimeOffset now)
    {
        QualityScore = qualityScore;
        BiasScore = biasScore;
        AiMatchScore = aiMatchScore;
        UpdatedAt = now;
    }

    public string GetDisplayPrice()
    {
        if (HourlyRate.HasValue)
            return $"${HourlyRate:F2}/hr";
        if (FixedPrice.HasValue)
            return $"${FixedPrice:F2}";
        return "Not specified";
    }

    public int GetDaysSinceSubmission(DateTimeOffset now)
    {
        return (int)(now - SubmittedAt).TotalDays;
    }

    public bool IsRevealed => RevealedAt.HasValue;
}

public enum BlindApplicationStatus
{
    Submitted = 0,
    Shortlisted = 1,
    Rejected = 2,
    Accepted = 3,
    Withdrawn = 4,
    Archived = 5
}
