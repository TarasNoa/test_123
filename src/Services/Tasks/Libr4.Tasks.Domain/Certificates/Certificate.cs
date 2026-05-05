using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Certificates;

public sealed class Certificate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public CertificateType CertificateType { get; private set; }
    public string IssuingOrganization { get; private set; } = "";
    public string? CertificateUrl { get; private set; }
    public string? CredentialId { get; private set; }
    public List<string> Skills { get; private set; } = new();
    public List<string> Tags { get; private set; } = new();
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public CertificateStatus Status { get; private set; }
    public DateTimeOffset IssuedDate { get; private set; }
    public DateTimeOffset? ExpiryDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public string? VerificationNotes { get; private set; }

    private readonly List<CertificateVerification> _verifications = new();
    private readonly List<CertificateEndorsement> _endorsements = new();
    private readonly List<CertificateAttachment> _attachments = new();

    public IReadOnlyCollection<CertificateVerification> Verifications => _verifications.AsReadOnly();
    public IReadOnlyCollection<CertificateEndorsement> Endorsements => _endorsements.AsReadOnly();
    public IReadOnlyCollection<CertificateAttachment> Attachments => _attachments.AsReadOnly();

    private Certificate() { }

    public static Certificate Create(
        Guid userId,
        string title,
        string description,
        CertificateType certificateType,
        string issuingOrganization,
        string? certificateUrl,
        string? credentialId,
        List<string>? skills,
        List<string>? tags,
        DateTimeOffset issuedDate,
        DateTimeOffset? expiryDate,
        DateTimeOffset now)
    {
        return new Certificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Description = description.Trim(),
            CertificateType = certificateType,
            IssuingOrganization = issuingOrganization.Trim(),
            CertificateUrl = certificateUrl?.Trim(),
            CredentialId = credentialId?.Trim(),
            Skills = skills ?? new(),
            Tags = tags ?? new(),
            Metadata = new(),
            Status = CertificateStatus.Active,
            IssuedDate = issuedDate,
            ExpiryDate = expiryDate,
            CreatedAt = now,
            UpdatedAt = now,
            VerifiedAt = null,
            VerifiedBy = null,
            VerificationNotes = null
        };
    }

    public void Update(
        string title,
        string description,
        CertificateType certificateType,
        string issuingOrganization,
        string? certificateUrl,
        string? credentialId,
        List<string>? skills,
        List<string>? tags,
        DateTimeOffset? expiryDate,
        DateTimeOffset now)
    {
        Title = title.Trim();
        Description = description.Trim();
        CertificateType = certificateType;
        IssuingOrganization = issuingOrganization.Trim();
        CertificateUrl = certificateUrl?.Trim();
        CredentialId = credentialId?.Trim();
        Skills = skills ?? new();
        Tags = tags ?? new();
        ExpiryDate = expiryDate;
        UpdatedAt = now;
    }

    public void Verify(Guid verifierId, string? notes, DateTimeOffset now)
    {
        Status = CertificateStatus.Active;
        VerifiedAt = now;
        VerifiedBy = verifierId;
        VerificationNotes = notes?.Trim();
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = CertificateStatus.Revoked;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = CertificateStatus.Suspended;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (ExpiryDate.HasValue && ExpiryDate <= now)
            Status = CertificateStatus.Expired;
        UpdatedAt = now;
    }

    public void AddVerification(Guid verifierId, string status, string? notes, DateTimeOffset now)
    {
        var verification = new CertificateVerification(Id, verifierId, status, notes, now);
        _verifications.Add(verification);
        UpdatedAt = now;
    }

    public void AddEndorsement(Guid endorserId, string endorsementText, DateTimeOffset now)
    {
        var endorsement = new CertificateEndorsement(Id, endorserId, endorsementText, now);
        _endorsements.Add(endorsement);
        UpdatedAt = now;
    }

    public void AddAttachment(string filename, string filePath, long fileSize, string mimeType, string? description, DateTimeOffset now)
    {
        var attachment = new CertificateAttachment(Id, filename, filePath, fileSize, mimeType, description, now);
        _attachments.Add(attachment);
        UpdatedAt = now;
    }

    public void AddMetadata(string key, object value, DateTimeOffset now)
    {
        Metadata[key] = value;
        UpdatedAt = now;
    }
}

public sealed class CertificateVerification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CertificateId { get; private set; }
    public Guid VerifierId { get; private set; }
    public string Status { get; private set; } = "";
    public string? VerificationNotes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CertificateVerification() { }

    internal CertificateVerification(Guid certificateId, Guid verifierId, string status, string? notes, DateTimeOffset now)
    {
        CertificateId = certificateId;
        VerifierId = verifierId;
        Status = status.Trim();
        VerificationNotes = notes?.Trim();
        CreatedAt = now;
    }
}

public sealed class CertificateEndorsement
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CertificateId { get; private set; }
    public Guid EndorserId { get; private set; }
    public string EndorsementText { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    private CertificateEndorsement() { }

    internal CertificateEndorsement(Guid certificateId, Guid endorserId, string endorsementText, DateTimeOffset now)
    {
        CertificateId = certificateId;
        EndorserId = endorserId;
        EndorsementText = endorsementText.Trim();
        CreatedAt = now;
    }
}

public sealed class CertificateAttachment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CertificateId { get; private set; }
    public string Filename { get; private set; } = "";
    public string FilePath { get; private set; } = "";
    public long FileSize { get; private set; }
    public string MimeType { get; private set; } = "";
    public string? Description { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    private CertificateAttachment() { }

    internal CertificateAttachment(Guid certificateId, string filename, string filePath, long fileSize, string mimeType, string? description, DateTimeOffset now)
    {
        CertificateId = certificateId;
        Filename = filename.Trim();
        FilePath = filePath.Trim();
        FileSize = fileSize;
        MimeType = mimeType.Trim();
        Description = description?.Trim();
        UploadedAt = now;
    }
}

public enum CertificateStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Revoked = 3,
    Suspended = 4
}

public enum CertificateType
{
    Skill = 0,
    Course = 1,
    Professional = 2,
    Academic = 3,
    Certification = 4,
    Achievement = 5
}
