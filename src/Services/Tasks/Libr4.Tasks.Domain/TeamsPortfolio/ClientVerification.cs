using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TeamsPortfolio;

public sealed class ClientVerification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string VerificationType { get; private set; } = "";
    public Dictionary<string, object> Documents { get; private set; } = new();
    public string? BusinessName { get; private set; }
    public string? BusinessAddress { get; private set; }
    public string? BusinessPhone { get; private set; }
    public string? BusinessEmail { get; private set; }
    public string? Website { get; private set; }
    public string? TaxId { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? BusinessType { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? VerificationNotes { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? BadgeLevel { get; private set; }
    public string? BadgeUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ClientVerification() { }

    public static ClientVerification Create(
        Guid userId,
        string verificationType,
        DateTimeOffset submittedAt)
    {
        return new ClientVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VerificationType = verificationType,
            Status = VerificationStatus.Pending,
            SubmittedAt = submittedAt,
            CreatedAt = submittedAt,
            UpdatedAt = submittedAt
        };
    }

    public void SetBusinessInfo(string? businessName, string? address, string? phone, string? email, string? website, DateTimeOffset now)
    {
        BusinessName = businessName;
        BusinessAddress = address;
        BusinessPhone = phone;
        BusinessEmail = email;
        Website = website;
        UpdatedAt = now;
    }

    public void SetLegalInfo(string? taxId, string? registrationNumber, string? businessType, DateTimeOffset now)
    {
        TaxId = taxId;
        RegistrationNumber = registrationNumber;
        BusinessType = businessType;
        UpdatedAt = now;
    }

    public void SetDocuments(Dictionary<string, object> documents, DateTimeOffset now)
    {
        Documents = documents ?? new();
        UpdatedAt = now;
    }

    public void Approve(Guid reviewedBy, string? badgeLevel, string? badgeUrl, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        Status = VerificationStatus.Verified;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
        BadgeLevel = badgeLevel;
        BadgeUrl = badgeUrl;
        ExpiresAt = expiresAt;
        UpdatedAt = now;
    }

    public void Reject(Guid reviewedBy, string rejectionReason, DateTimeOffset now)
    {
        Status = VerificationStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
        RejectionReason = rejectionReason;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        Status = VerificationStatus.Expired;
        UpdatedAt = now;
    }

    public bool IsVerified(DateTimeOffset now)
    {
        return Status == VerificationStatus.Verified && (!ExpiresAt.HasValue || ExpiresAt.Value > now);
    }
}

public sealed class PortfolioAnalytics : AggregateRoot<Guid>
{
    public Guid PortfolioItemId { get; private set; }
    public int Views { get; private set; }
    public int UniqueViews { get; private set; }
    public int Clicks { get; private set; }
    public int Conversions { get; private set; }
    public float? AverageViewDuration { get; private set; }
    public float? BounceRate { get; private set; }
    public Dictionary<string, object> ViewsByCountry { get; private set; } = new();
    public Dictionary<string, object> ViewsBySource { get; private set; } = new();
    public Dictionary<string, object> DailyViews { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PortfolioAnalytics() { }

    public static PortfolioAnalytics Create(Guid portfolioItemId, DateTimeOffset now)
    {
        return new PortfolioAnalytics
        {
            Id = Guid.NewGuid(),
            PortfolioItemId = portfolioItemId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RecordView(string? country, string? source, DateTimeOffset now)
    {
        Views++;
        UniqueViews++;

        if (!string.IsNullOrEmpty(country))
        {
            var key = country;
            if (ViewsByCountry.ContainsKey(key))
                ViewsByCountry[key] = ((int?)ViewsByCountry[key] ?? 0) + 1;
            else
                ViewsByCountry[key] = 1;
        }

        if (!string.IsNullOrEmpty(source))
        {
            var key = source;
            if (ViewsBySource.ContainsKey(key))
                ViewsBySource[key] = ((int?)ViewsBySource[key] ?? 0) + 1;
            else
                ViewsBySource[key] = 1;
        }

        UpdatedAt = now;
    }

    public void RecordClick()
    {
        Clicks++;
    }

    public void RecordConversion()
    {
        Conversions++;
    }

    public void SetMetrics(float? avgDuration, float? bounceRate, DateTimeOffset now)
    {
        AverageViewDuration = avgDuration;
        BounceRate = bounceRate;
        UpdatedAt = now;
    }

    public void SetDailyViews(Dictionary<string, object> dailyViews, DateTimeOffset now)
    {
        DailyViews = dailyViews ?? new();
        UpdatedAt = now;
    }

    public float GetConversionRate()
    {
        if (Views == 0)
            return 0.0f;
        return (Conversions / (float)Views) * 100;
    }
}

public enum VerificationStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2,
    Expired = 3
}
