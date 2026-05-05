using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Gdpr;

public sealed class GdprRequest : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public GdprRequestType Type { get; private set; }
    public GdprRequestStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? ExportFileUrl { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }

    private GdprRequest() { }

    public static GdprRequest Submit(Guid userId, GdprRequestType type, string? reason, DateTimeOffset now, TimeSpan? gracePeriod = null)
    {
        return new GdprRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Status = GdprRequestStatus.Pending,
            Reason = reason,
            RequestedAt = now,
            ScheduledFor = type == GdprRequestType.Erasure ? now.Add(gracePeriod ?? TimeSpan.FromDays(30)) : null
        };
    }

    public void Process(string? exportUrl, DateTimeOffset now)
    {
        Status = GdprRequestStatus.Completed;
        ExportFileUrl = exportUrl;
        ProcessedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != GdprRequestStatus.Pending) return;
        Status = GdprRequestStatus.Cancelled;
        ProcessedAt = now;
    }

    public void MarkFailed(DateTimeOffset now)
    {
        Status = GdprRequestStatus.Failed;
        ProcessedAt = now;
    }
}

public sealed class ConsentRecord : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public ConsentType Type { get; private set; }
    public string Version { get; private set; } = "";
    public bool Granted { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private ConsentRecord() { }

    public static ConsentRecord Record(Guid userId, ConsentType type, string version, bool granted,
        string? ip, string? ua, DateTimeOffset now)
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Version = version,
            Granted = granted,
            IpAddress = ip,
            UserAgent = ua,
            RecordedAt = now
        };
    }
}

public enum GdprRequestType { Export = 0, Erasure = 1, Rectification = 2, Restriction = 3, Portability = 4 }
public enum GdprRequestStatus { Pending = 0, Processing = 1, Completed = 2, Cancelled = 3, Failed = 4 }
public enum ConsentType { TermsOfService = 0, PrivacyPolicy = 1, Marketing = 2, Cookies = 3, DataProcessing = 4, ThirdPartySharing = 5 }
