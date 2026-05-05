using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TeamsPortfolio;

public sealed class FreelancerTeam : AggregateRoot<Guid>
{
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public string? Tagline { get; private set; }
    public string? Website { get; private set; }
    public string? Location { get; private set; }
    public string? Timezone { get; private set; }
    public List<string> Languages { get; private set; } = new();
    public List<string> Skills { get; private set; } = new();
    public List<string> Industries { get; private set; } = new();
    public List<string> Categories { get; private set; } = new();
    public int? MinProjectSize { get; private set; }
    public float? HourlyRateMin { get; private set; }
    public float? HourlyRateMax { get; private set; }
    public string? PreferredRateType { get; private set; }
    public int CompletedProjects { get; private set; }
    public float TotalEarnings { get; private set; }
    public float? AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsVerified { get; private set; }
    public bool IsFeatured { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public Dictionary<string, object> BrandColors { get; private set; } = new();
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<TeamMember> _members = new();
    private readonly List<PortfolioItem> _portfolioItems = new();

    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<PortfolioItem> PortfolioItems => _portfolioItems.AsReadOnly();

    private FreelancerTeam() { }

    public static FreelancerTeam Create(
        string name,
        string? description,
        string? tagline,
        Guid createdBy,
        DateTimeOffset now)
    {
        return new FreelancerTeam
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Tagline = tagline?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateBasicInfo(string name, string? description, string? tagline, DateTimeOffset now)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Tagline = tagline?.Trim();
        UpdatedAt = now;
    }

    public void UpdateLocation(string? location, string? timezone, DateTimeOffset now)
    {
        Location = location;
        Timezone = timezone;
        UpdatedAt = now;
    }

    public void UpdateRates(float? minHourly, float? maxHourly, int? minProject, string? preferredType, DateTimeOffset now)
    {
        HourlyRateMin = minHourly;
        HourlyRateMax = maxHourly;
        MinProjectSize = minProject;
        PreferredRateType = preferredType;
        UpdatedAt = now;
    }

    public void UpdateBranding(string? logoUrl, string? bannerUrl, Dictionary<string, object>? colors, DateTimeOffset now)
    {
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
        BrandColors = colors ?? new();
        UpdatedAt = now;
    }

    public void AddMember(Guid userId, TeamRole role, string? title, Guid? invitedBy, DateTimeOffset now)
    {
        var member = new TeamMember(Id, userId, role, title, invitedBy, now);
        _members.Add(member);
        UpdatedAt = now;
    }

    public void AddPortfolioItem(Guid itemId, DateTimeOffset now)
    {
        var item = new PortfolioItem(itemId, Id, now);
        _portfolioItems.Add(item);
        UpdatedAt = now;
    }

    public void SetVerified(bool verified, DateTimeOffset now)
    {
        IsVerified = verified;
        UpdatedAt = now;
    }

    public void SetFeatured(bool featured, DateTimeOffset now)
    {
        IsFeatured = featured;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    public int GetActiveMemberCount()
    {
        return _members.Count(m => m.Status == TeamMemberStatus.Active);
    }

    public bool IsAvailable => IsActive && GetActiveMemberCount() > 0;
}

public sealed class TeamMember
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamRole Role { get; private set; }
    public string? Title { get; private set; }
    public string? Bio { get; private set; }
    public List<string> Permissions { get; private set; } = new();
    public TeamMemberStatus Status { get; private set; } = TeamMemberStatus.Active;
    public Guid? InvitedBy { get; private set; }
    public DateTimeOffset? InvitedAt { get; private set; }
    public DateTimeOffset? JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }
    public int ProjectsContributed { get; private set; }
    public float EarningsContributed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TeamMember() { }

    internal TeamMember(Guid teamId, Guid userId, TeamRole role, string? title, Guid? invitedBy, DateTimeOffset now)
    {
        TeamId = teamId;
        UserId = userId;
        Role = role;
        Title = title;
        InvitedBy = invitedBy;
        InvitedAt = now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Accept(DateTimeOffset now)
    {
        Status = TeamMemberStatus.Active;
        JoinedAt = now;
        UpdatedAt = now;
    }

    public void UpdateRole(TeamRole role, DateTimeOffset now)
    {
        Role = role;
        UpdatedAt = now;
    }

    public void UpdatePermissions(List<string> permissions, DateTimeOffset now)
    {
        Permissions = permissions ?? new();
        UpdatedAt = now;
    }

    public void Leave(DateTimeOffset now)
    {
        Status = TeamMemberStatus.Removed;
        LeftAt = now;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = TeamMemberStatus.Suspended;
        UpdatedAt = now;
    }

    public bool IsActive => Status == TeamMemberStatus.Active;
    public bool CanManageTeam => Role == TeamRole.Lead || Permissions.Contains("manage_team");
}

public sealed class PortfolioItem
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? TeamId { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public List<string> Images { get; private set; } = new();
    public List<string> Videos { get; private set; } = new();
    public List<string> Files { get; private set; } = new();
    public string? ProjectUrl { get; private set; }
    public string? ClientName { get; private set; }
    public string? ClientTestimonial { get; private set; }
    public string? ProjectDuration { get; private set; }
    public List<string> Technologies { get; private set; } = new();
    public List<string> ToolsUsed { get; private set; } = new();
    public List<string> Methodologies { get; private set; } = new();
    public Dictionary<string, object> BudgetRange { get; private set; } = new();
    public int? TeamSize { get; private set; }
    public string? RoleInProject { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public int ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int ShareCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private PortfolioItem() { }

    internal PortfolioItem(Guid id, Guid? teamId, DateTimeOffset now)
    {
        Id = id;
        TeamId = teamId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        IsActive = true;
        IsPublic = true;
        PublishedAt = now;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void AddView()
    {
        ViewCount++;
    }

    public void AddLike()
    {
        LikeCount++;
    }

    public void AddShare()
    {
        ShareCount++;
    }

    public bool IsPublished => IsActive && IsPublic && PublishedAt.HasValue;
}

public enum TeamRole
{
    Lead = 0,
    Member = 1,
    SeniorMember = 2,
    JuniorMember = 3
}

public enum TeamMemberStatus
{
    Active = 0,
    Invited = 1,
    Pending = 2,
    Suspended = 3,
    Removed = 4
}
