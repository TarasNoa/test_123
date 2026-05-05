using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.AML;

public class SanctionsScreening : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    // Screening results
    public bool IsSanctioned { get; private set; }
    public string? SanctionList { get; private set; }

    // Match details
    public float? MatchScore { get; private set; }
    public string? MatchedName { get; private set; }
    public string? SanctionType { get; private set; }
    public DateTimeOffset? EffectiveDate { get; private set; }

    // Status
    public bool IsConfirmed { get; private set; }
    public bool IsFalsePositive { get; private set; }

    // Action taken
    public bool AccountFrozen { get; private set; }
    public bool ReportedToAuthorities { get; private set; }

    public DateTimeOffset ScreeningDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SanctionsScreening() { }

    public static SanctionsScreening Create(Guid userId, DateTimeOffset now)
    {
        return new SanctionsScreening
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScreeningDate = now,
            CreatedAt = now
        };
    }

    public void SetSanctionStatus(bool isSanctioned, string? sanctionList, DateTimeOffset now)
    {
        IsSanctioned = isSanctioned;
        SanctionList = sanctionList;
        CreatedAt = now;
    }

    public void SetMatchDetails(float matchScore, string? matchedName, string? sanctionType, DateTimeOffset? effectiveDate, DateTimeOffset now)
    {
        MatchScore = matchScore;
        MatchedName = matchedName;
        SanctionType = sanctionType;
        EffectiveDate = effectiveDate;
        CreatedAt = now;
    }

    public void SetConfirmation(bool confirmed, DateTimeOffset now)
    {
        IsConfirmed = confirmed;
        CreatedAt = now;
    }

    public void SetFalsePositive(bool isFalsePositive, DateTimeOffset now)
    {
        IsFalsePositive = isFalsePositive;
        CreatedAt = now;
    }

    public void FreezeAccount(bool frozen, DateTimeOffset now)
    {
        AccountFrozen = frozen;
        CreatedAt = now;
    }

    public void ReportToAuthorities(bool reported, DateTimeOffset now)
    {
        ReportedToAuthorities = reported;
        CreatedAt = now;
    }
}
