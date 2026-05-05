using Libr4.Shared.Kernel.Domain;

namespace Libr4.CRM.Domain.Profile;

public enum ProfileVisibility
{
    Public,
    Private,
    ConnectionsOnly
}

public class UserProfile : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Location { get; private set; }
    public string? Website { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? GitHubUrl { get; private set; }
    public string? TwitterUrl { get; private set; }
    public ProfileVisibility Visibility { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Skill> _skills = new();
    public IReadOnlyCollection<Skill> Skills => _skills.AsReadOnly();

    private readonly List<Experience> _experiences = new();
    public IReadOnlyCollection<Experience> Experiences => _experiences.AsReadOnly();

    private readonly List<Education> _educations = new();
    public IReadOnlyCollection<Education> Educations => _educations.AsReadOnly();

    private UserProfile() { }

    public static UserProfile Create(Guid userId, string? displayName = null, string? bio = null, ProfileVisibility visibility = ProfileVisibility.Public)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName,
            Bio = bio,
            Visibility = visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBio(string bio)
    {
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(string location)
    {
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSocialLinks(string? website = null, string? linkedInUrl = null, string? gitHubUrl = null, string? twitterUrl = null)
    {
        Website = website;
        LinkedInUrl = linkedInUrl;
        GitHubUrl = gitHubUrl;
        TwitterUrl = twitterUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVisibility(ProfileVisibility visibility)
    {
        Visibility = visibility;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSkill(Skill skill)
    {
        _skills.Add(skill);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveSkill(Guid skillId)
    {
        var skill = _skills.FirstOrDefault(s => s.Id == skillId);
        if (skill != null)
        {
            _skills.Remove(skill);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddExperience(Experience experience)
    {
        _experiences.Add(experience);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveExperience(Guid experienceId)
    {
        var experience = _experiences.FirstOrDefault(e => e.Id == experienceId);
        if (experience != null)
        {
            _experiences.Remove(experience);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddEducation(Education education)
    {
        _educations.Add(education);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveEducation(Guid educationId)
    {
        var education = _educations.FirstOrDefault(e => e.Id == educationId);
        if (education != null)
        {
            _educations.Remove(education);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

public class Skill : Entity<Guid>
{
    public Guid UserProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int ProficiencyLevel { get; private set; }  // 1-5
    public int YearsOfExperience { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Skill() { }

    public static Skill Create(Guid userProfileId, string name, string? description = null, int proficiencyLevel = 3, int yearsOfExperience = 0)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Name = name,
            Description = description,
            ProficiencyLevel = proficiencyLevel,
            YearsOfExperience = yearsOfExperience,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProficiency(int proficiencyLevel, int yearsOfExperience)
    {
        ProficiencyLevel = proficiencyLevel;
        YearsOfExperience = yearsOfExperience;
    }

    public void Verify()
    {
        IsVerified = true;
    }
}

public class Experience : Entity<Guid>
{
    public Guid UserProfileId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Experience() { }

    public static Experience Create(Guid userProfileId, string companyName, string position, DateTime startDate, DateTime? endDate = null, string? description = null)
    {
        return new Experience
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            CompanyName = companyName,
            Position = position,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = endDate == null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void EndPosition(DateTime endDate)
    {
        EndDate = endDate;
        IsCurrent = false;
    }
}

public class Education : Entity<Guid>
{
    public Guid UserProfileId { get; private set; }
    public string Institution { get; private set; } = string.Empty;
    public string Degree { get; private set; } = string.Empty;
    public string FieldOfStudy { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Education() { }

    public static Education Create(Guid userProfileId, string institution, string degree, string fieldOfStudy, DateTime startDate, DateTime? endDate = null)
    {
        return new Education
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Institution = institution,
            Degree = degree,
            FieldOfStudy = fieldOfStudy,
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = endDate == null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(DateTime endDate)
    {
        EndDate = endDate;
        IsCurrent = false;
    }
}
