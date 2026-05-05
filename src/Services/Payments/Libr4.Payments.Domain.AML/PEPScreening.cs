using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.AML;

public class PEPScreening : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    // Screening results
    public bool IsPep { get; private set; }
    public string? PepCategory { get; private set; }

    // Match details
    public float? MatchScore { get; private set; }
    public string? MatchedName { get; private set; }
    public string? Position { get; private set; }
    public string? Country { get; private set; }

    // Source
    public string? DataSource { get; private set; }
    public DateTimeOffset ScreeningDate { get; private set; }

    // Status
    public bool IsConfirmed { get; private set; }
    public bool RequiresEnhancedDD { get; private set; }

    // Notes
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PEPScreening() { }

    public static PEPScreening Create(Guid userId, DateTimeOffset now)
    {
        return new PEPScreening
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScreeningDate = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetPepStatus(bool isPep, string? category, DateTimeOffset now)
    {
        IsPep = isPep;
        PepCategory = category;
        UpdatedAt = now;
    }

    public void SetMatchDetails(float matchScore, string? matchedName, string? position, string? country, DateTimeOffset now)
    {
        MatchScore = matchScore;
        MatchedName = matchedName;
        Position = position;
        Country = country;
        UpdatedAt = now;
    }

    public void SetDataSource(string dataSource, DateTimeOffset now)
    {
        DataSource = dataSource;
        UpdatedAt = now;
    }

    public void SetConfirmation(bool confirmed, DateTimeOffset now)
    {
        IsConfirmed = confirmed;
        UpdatedAt = now;
    }

    public void SetEnhancedDD(bool required, DateTimeOffset now)
    {
        RequiresEnhancedDD = required;
        UpdatedAt = now;
    }

    public void SetNotes(string? notes, DateTimeOffset now)
    {
        Notes = notes;
        UpdatedAt = now;
    }
}
