using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Kyc;

public sealed class KycVerification : AggregateRoot<Guid>
{
    private readonly List<KycDocument> _documents = new();
    private readonly List<KycCheck> _checks = new();

    public Guid UserId { get; private set; }
    public KycLevel Level { get; private set; }
    public KycStatus Status { get; private set; }
    public string? Provider { get; private set; }
    public string? ExternalRefId { get; private set; }
    public string? FullName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Nationality { get; private set; }
    public string? CountryOfResidence { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public RiskRating RiskRating { get; private set; }
    public bool IsPep { get; private set; }
    public bool SanctionsHit { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public IReadOnlyCollection<KycDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<KycCheck> Checks => _checks.AsReadOnly();

    private KycVerification() { }

    public static KycVerification Initiate(Guid userId, KycLevel level, string provider, DateTimeOffset now)
    {
        return new KycVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Level = level,
            Status = KycStatus.Pending,
            Provider = provider,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void SubmitPersonalData(string fullName, DateOnly dob, string nationality, string country,
        string addressLine1, string? addressLine2, string city, string postalCode, DateTimeOffset now)
    {
        FullName = fullName;
        DateOfBirth = dob;
        Nationality = nationality;
        CountryOfResidence = country;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        PostalCode = postalCode;
        Status = KycStatus.UnderReview;
        UpdatedAt = now;
    }

    public KycDocument AddDocument(KycDocumentType type, string fileUrl, string? country, DateTimeOffset now)
    {
        var doc = new KycDocument(Id, type, fileUrl, country, now);
        _documents.Add(doc);
        UpdatedAt = now;
        return doc;
    }

    public KycCheck RecordCheck(KycCheckType type, KycCheckResult result, string? details, DateTimeOffset now)
    {
        var check = new KycCheck(Id, type, result, details, now);
        _checks.Add(check);
        if (result == KycCheckResult.SanctionsHit) SanctionsHit = true;
        UpdatedAt = now;
        return check;
    }

    public void Approve(RiskRating risk, bool isPep, DateTimeOffset now, TimeSpan? validity = null)
    {
        Status = KycStatus.Approved;
        RiskRating = risk;
        IsPep = isPep;
        VerifiedAt = now;
        ExpiresAt = now.Add(validity ?? TimeSpan.FromDays(365));
        UpdatedAt = now;
    }

    public void Reject(string reason, DateTimeOffset now)
    {
        Status = KycStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = now;
    }

    public void RequestAdditional(string reason, DateTimeOffset now)
    {
        Status = KycStatus.AdditionalInfoRequired;
        RejectionReason = reason;
        UpdatedAt = now;
    }

    public void LinkExternal(string externalRef, DateTimeOffset now)
    {
        ExternalRefId = externalRef;
        UpdatedAt = now;
    }
}

public sealed class KycDocument
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VerificationId { get; private set; }
    public KycDocumentType Type { get; private set; }
    public string FileUrl { get; private set; } = "";
    public string? Country { get; private set; }
    public KycCheckResult? VerificationResult { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    private KycDocument() { }
    internal KycDocument(Guid verificationId, KycDocumentType type, string fileUrl, string? country, DateTimeOffset now)
    {
        VerificationId = verificationId;
        Type = type;
        FileUrl = fileUrl;
        Country = country;
        UploadedAt = now;
    }

    public void RecordResult(KycCheckResult result) => VerificationResult = result;
}

public sealed class KycCheck
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VerificationId { get; private set; }
    public KycCheckType Type { get; private set; }
    public KycCheckResult Result { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }

    private KycCheck() { }
    internal KycCheck(Guid verificationId, KycCheckType type, KycCheckResult result, string? details, DateTimeOffset now)
    {
        VerificationId = verificationId;
        Type = type;
        Result = result;
        Details = details;
        PerformedAt = now;
    }
}

public enum KycLevel { Basic = 0, Standard = 1, Enhanced = 2 }
public enum KycStatus { Pending = 0, UnderReview = 1, AdditionalInfoRequired = 2, Approved = 3, Rejected = 4, Expired = 5 }
public enum KycDocumentType { Passport = 0, IdCard = 1, DriverLicense = 2, ProofOfAddress = 3, Selfie = 4, Other = 99 }
public enum KycCheckType { IdentityDocument = 0, FaceMatch = 1, AddressVerification = 2, SanctionsScreening = 3, PepScreening = 4, AmlRisk = 5 }
public enum KycCheckResult { Pass = 0, Warn = 1, Fail = 2, SanctionsHit = 3, ManualReview = 4 }
public enum RiskRating { Low = 0, Medium = 1, High = 2, Prohibited = 3 }
