using Libr4.Shared.Kernel.Domain;

namespace Libr4.Education.Domain.Academy;

public enum CourseStatus
{
    Draft,
    Published,
    Archived
}

public enum EnrollmentStatus
{
    Active,
    Completed,
    Dropped,
    Expired
}

public class Course : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CourseStatus Status { get; private set; }
    public decimal Price { get; private set; }
    public int DurationHours { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public int EnrollmentCount { get; private set; }
    public float AverageRating { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private readonly List<CourseModule> _modules = new();
    public IReadOnlyCollection<CourseModule> Modules => _modules.AsReadOnly();

    private readonly List<Enrollment> _enrollments = new();
    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.AsReadOnly();

    private Course() { }

    public static Course Create(
        string title,
        string description,
        decimal price,
        int durationHours,
        string category,
        string? thumbnailUrl = null)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = CourseStatus.Draft,
            Price = price,
            DurationHours = durationHours,
            ThumbnailUrl = thumbnailUrl,
            Category = category,
            EnrollmentCount = 0,
            AverageRating = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddModule(CourseModule module)
    {
        _modules.Add(module);
    }

    public void Enroll(Enrollment enrollment)
    {
        _enrollments.Add(enrollment);
        EnrollmentCount++;
    }

    public void Publish()
    {
        Status = CourseStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = CourseStatus.Archived;
    }

    public void UpdateRating(float rating)
    {
        AverageRating = rating;
    }
}

public class CourseModule : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? Content { get; private set; }

    private CourseModule() { }

    public static CourseModule Create(
        Guid courseId,
        string title,
        string description,
        int order,
        int durationMinutes,
        string? videoUrl = null,
        string? content = null)
    {
        return new CourseModule
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = title,
            Description = description,
            Order = order,
            DurationMinutes = durationMinutes,
            VideoUrl = videoUrl,
            Content = content
        };
    }

    public void UpdateOrder(int order)
    {
        Order = order;
    }
}

public class Enrollment : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public float Progress { get; private set; }
    public DateTimeOffset EnrolledAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? LastAccessedAt { get; private set; }

    private Enrollment() { }

    public static Enrollment Create(Guid courseId, Guid userId)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            Status = EnrollmentStatus.Active,
            Progress = 0,
            EnrolledAt = DateTimeOffset.UtcNow,
            LastAccessedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProgress(float progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        LastAccessedAt = DateTimeOffset.UtcNow;

        if (Progress >= 100 && Status != EnrollmentStatus.Completed)
        {
            Complete();
        }
    }

    public void Complete()
    {
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        Progress = 100;
    }

    public void Drop()
    {
        Status = EnrollmentStatus.Dropped;
    }

    public void Expire()
    {
        Status = EnrollmentStatus.Expired;
    }
}

public class Certificate : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public string CertificateNumber { get; private set; } = string.Empty;
    public string? IssuerName { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsValid => !ExpiresAt.HasValue || ExpiresAt > DateTimeOffset.UtcNow;

    private Certificate() { }

    public static Certificate Create(Guid userId, Guid courseId, string certificateNumber, string? issuerName = null, DateTimeOffset? expiresAt = null)
    {
        return new Certificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            CertificateNumber = certificateNumber,
            IssuerName = issuerName,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke()
    {
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
    }

    public void Renew(DateTimeOffset newExpiryDate)
    {
        ExpiresAt = newExpiryDate;
    }
}

public class Skill : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ProficiencyLevel { get; private set; } // 1-10
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Skill() { }

    public static Skill Create(string name, string category, string description, int proficiencyLevel = 1)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Description = description,
            ProficiencyLevel = Math.Clamp(proficiencyLevel, 1, 10),
            IsVerified = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProficiency(int level)
    {
        ProficiencyLevel = Math.Clamp(level, 1, 10);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        VerifiedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
