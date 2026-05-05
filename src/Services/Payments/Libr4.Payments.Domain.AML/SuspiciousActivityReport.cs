using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.AML;

public class SuspiciousActivityReport : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? TransactionId { get; private set; }

    // Report details
    public string ReportType { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "medium";

    // Description
    public string Description { get; private set; } = string.Empty;

    // Activity details
    public string? ActivityType { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public string? Location { get; private set; }
    public string? IpAddress { get; private set; }

    // Status
    public string Status { get; private set; } = "pending";
    public bool IsResolved { get; private set; }
    public bool IsFalsePositive { get; private set; }

    // Resolution
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    // Reporting
    public bool ReportedToAuthorities { get; private set; }
    public string? ReportReference { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SuspiciousActivityReport() { }

    public static SuspiciousActivityReport Create(
        Guid userId,
        string reportType,
        string description,
        DateTimeOffset now)
    {
        return new SuspiciousActivityReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReportType = reportType,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetTransaction(Guid? transactionId, DateTimeOffset now)
    {
        TransactionId = transactionId;
        UpdatedAt = now;
    }

    public void SetPriority(string priority, DateTimeOffset now)
    {
        Priority = priority;
        UpdatedAt = now;
    }

    public void SetActivityDetails(
        string? activityType,
        decimal? amount,
        string? currency,
        string? location,
        string? ipAddress,
        DateTimeOffset now)
    {
        ActivityType = activityType;
        Amount = amount;
        Currency = currency;
        Location = location;
        IpAddress = ipAddress;
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void Resolve(bool isFalsePositive, string? notes, DateTimeOffset now)
    {
        IsResolved = true;
        IsFalsePositive = isFalsePositive;
        ResolutionNotes = notes;
        ResolvedAt = now;
        UpdatedAt = now;
    }

    public void ReportToAuthority(bool reported, string? reference, DateTimeOffset now)
    {
        ReportedToAuthorities = reported;
        ReportReference = reference;
        UpdatedAt = now;
    }
}
