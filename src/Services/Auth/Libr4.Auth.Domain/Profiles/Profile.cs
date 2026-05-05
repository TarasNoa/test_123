using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Profiles;

public sealed class UserProfile : AggregateRoot<Guid>
{
    private readonly List<ProfileSkill> _skills = new();
    private readonly List<ProfileLanguage> _languages = new();
    private readonly List<ProfileSocialLink> _socials = new();

    public Guid UserId { get; private set; }
    public string? Headline { get; private set; }
    public string? Bio { get; private set; }
    public string? Location { get; private set; }
    public string? TimeZone { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public AvailabilityStatus Availability { get; private set; }
    public decimal? HourlyRate { get; private set; }
    public string? HourlyRateCurrency { get; private set; }
    public int ProfileCompletenessPct { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProfileSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<ProfileLanguage> Languages => _languages.AsReadOnly();
    public IReadOnlyCollection<ProfileSocialLink> Socials => _socials.AsReadOnly();

    private UserProfile() { }

    public static UserProfile Create(Guid userId, DateTimeOffset now)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Availability = AvailabilityStatus.Available,
            CreatedAt = now,
            UpdatedAt = now,
            ProfileCompletenessPct = 10
        };
    }

    public void UpdateBasics(string? headline, string? bio, string? location, string? timeZone, DateTimeOffset now)
    {
        Headline = headline?.Trim();
        Bio = bio?.Trim();
        Location = location?.Trim();
        TimeZone = timeZone?.Trim();
        UpdatedAt = now;
        RecalculateCompleteness();
    }

    public void UpdateAvatar(string? avatarUrl, string? coverUrl, DateTimeOffset now)
    {
        AvatarUrl = avatarUrl;
        CoverUrl = coverUrl;
        UpdatedAt = now;
        RecalculateCompleteness();
    }

    public void SetAvailability(AvailabilityStatus availability, decimal? hourlyRate, string? currency, DateTimeOffset now)
    {
        Availability = availability;
        HourlyRate = hourlyRate;
        HourlyRateCurrency = currency;
        UpdatedAt = now;
    }

    public void SetVisibility(bool isPublic, DateTimeOffset now)
    {
        IsPublic = isPublic;
        UpdatedAt = now;
    }

    public void AddSkill(string name, SkillLevel level, int yearsOfExperience)
    {
        if (_skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        _skills.Add(new ProfileSkill(Id, name.Trim(), level, yearsOfExperience));
        RecalculateCompleteness();
    }

    public void RemoveSkill(string name) => _skills.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void AddLanguage(string code, LanguageProficiency proficiency)
    {
        if (_languages.Any(l => l.Code == code)) return;
        _languages.Add(new ProfileLanguage(Id, code, proficiency));
        RecalculateCompleteness();
    }

    public void RemoveLanguage(string code) => _languages.RemoveAll(l => l.Code == code);

    public void AddSocial(SocialPlatform platform, string url)
    {
        _socials.RemoveAll(s => s.Platform == platform);
        _socials.Add(new ProfileSocialLink(Id, platform, url));
    }

    private void RecalculateCompleteness()
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(Headline)) score += 15;
        if (!string.IsNullOrWhiteSpace(Bio)) score += 20;
        if (!string.IsNullOrWhiteSpace(Location)) score += 5;
        if (!string.IsNullOrWhiteSpace(AvatarUrl)) score += 10;
        if (_skills.Count >= 3) score += 25;
        if (_languages.Count >= 1) score += 10;
        if (HourlyRate.HasValue) score += 10;
        if (_socials.Count >= 1) score += 5;
        ProfileCompletenessPct = Math.Min(100, score);
    }
}

public sealed class ProfileSkill
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = "";
    public SkillLevel Level { get; private set; }
    public int YearsOfExperience { get; private set; }
    public bool Verified { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    private ProfileSkill() { }
    internal ProfileSkill(Guid profileId, string name, SkillLevel level, int years)
    {
        ProfileId = profileId;
        Name = name;
        Level = level;
        YearsOfExperience = years;
    }

    public void MarkVerified(DateTimeOffset now)
    {
        Verified = true;
        VerifiedAt = now;
    }
}

public sealed class ProfileLanguage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProfileId { get; private set; }
    public string Code { get; private set; } = "";
    public LanguageProficiency Proficiency { get; private set; }

    private ProfileLanguage() { }
    internal ProfileLanguage(Guid profileId, string code, LanguageProficiency p)
    {
        ProfileId = profileId;
        Code = code.ToLowerInvariant();
        Proficiency = p;
    }
}

public sealed class ProfileSocialLink
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProfileId { get; private set; }
    public SocialPlatform Platform { get; private set; }
    public string Url { get; private set; } = "";

    private ProfileSocialLink() { }
    internal ProfileSocialLink(Guid profileId, SocialPlatform platform, string url)
    {
        ProfileId = profileId;
        Platform = platform;
        Url = url;
    }
}

public enum AvailabilityStatus { Available = 0, PartTime = 1, Busy = 2, NotAvailable = 3 }
public enum SkillLevel { Beginner = 0, Intermediate = 1, Advanced = 2, Expert = 3 }
public enum LanguageProficiency { Basic = 0, Conversational = 1, Fluent = 2, Native = 3 }
public enum SocialPlatform { LinkedIn = 0, GitHub = 1, Twitter = 2, Telegram = 3, Discord = 4, Behance = 5, Dribbble = 6, Website = 7, Other = 99 }
