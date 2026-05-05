using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Organizations;

public sealed class Organization : AggregateRoot<Guid>
{
    private readonly List<OrganizationMember> _members = new();
    private readonly List<OrganizationInvite> _invites = new();

    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public OrganizationPlan Plan { get; private set; }
    public int SeatLimit { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<OrganizationMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<OrganizationInvite> Invites => _invites.AsReadOnly();

    private Organization() { }

    public static Organization Create(string name, string slug, Guid ownerId, OrganizationPlan plan, DateTimeOffset now)
    {
        var seatLimit = plan switch
        {
            OrganizationPlan.Free => 3,
            OrganizationPlan.Team => 10,
            OrganizationPlan.Business => 50,
            OrganizationPlan.Enterprise => int.MaxValue,
            _ => 3
        };
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            OwnerId = ownerId,
            Plan = plan,
            SeatLimit = seatLimit,
            CreatedAt = now,
            UpdatedAt = now
        };
        org._members.Add(new OrganizationMember(org.Id, ownerId, OrgRole.Owner, now));
        return org;
    }

    public OrganizationInvite InviteUser(string email, OrgRole role, Guid invitedBy, string token, DateTimeOffset now, TimeSpan? lifetime = null)
    {
        if (_members.Count >= SeatLimit)
            throw new DomainException("Org.SeatLimitExceeded", "Seat limit exceeded for this organization plan");
        var invite = new OrganizationInvite(Id, email, role, invitedBy, token, now, lifetime ?? TimeSpan.FromDays(7));
        _invites.Add(invite);
        UpdatedAt = now;
        return invite;
    }

    public bool AcceptInvite(string tokenHash, Guid acceptingUserId, DateTimeOffset now)
    {
        var invite = _invites.FirstOrDefault(i => i.TokenHash == tokenHash && i.IsActive(now));
        if (invite is null) return false;
        if (_members.Count >= SeatLimit) return false;
        invite.Accept(now);
        _members.Add(new OrganizationMember(Id, acceptingUserId, invite.Role, now));
        UpdatedAt = now;
        return true;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId) throw new DomainException("Org.CannotRemoveOwner", "Cannot remove organization owner");
        _members.RemoveAll(m => m.UserId == userId);
    }

    public void ChangeMemberRole(Guid userId, OrgRole newRole)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) return;
        if (member.UserId == OwnerId && newRole != OrgRole.Owner) throw new DomainException("Org.OwnerRoleLocked", "Cannot change owner's role");
        member.ChangeRole(newRole);
    }

    public void ChangePlan(OrganizationPlan plan, DateTimeOffset now)
    {
        Plan = plan;
        SeatLimit = plan switch
        {
            OrganizationPlan.Free => 3,
            OrganizationPlan.Team => 10,
            OrganizationPlan.Business => 50,
            OrganizationPlan.Enterprise => int.MaxValue,
            _ => 3
        };
        UpdatedAt = now;
    }

    public void UpdateBranding(string? description, string? logoUrl, string? websiteUrl, DateTimeOffset now)
    {
        Description = description;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        UpdatedAt = now;
    }
}

public sealed class OrganizationMember
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrgRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private OrganizationMember() { }
    internal OrganizationMember(Guid orgId, Guid userId, OrgRole role, DateTimeOffset now)
    {
        OrganizationId = orgId;
        UserId = userId;
        Role = role;
        JoinedAt = now;
    }
    internal void ChangeRole(OrgRole newRole) => Role = newRole;
}

public sealed class OrganizationInvite
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = "";
    public OrgRole Role { get; private set; }
    public Guid InvitedBy { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }

    private OrganizationInvite() { }
    internal OrganizationInvite(Guid orgId, string email, OrgRole role, Guid invitedBy, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        OrganizationId = orgId;
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        InvitedBy = invitedBy;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = now.Add(lifetime);
    }

    public bool IsActive(DateTimeOffset now) => AcceptedAt is null && ExpiresAt > now;
    internal void Accept(DateTimeOffset now) => AcceptedAt = now;
}

public enum OrganizationPlan { Free = 0, Team = 1, Business = 2, Enterprise = 3 }
public enum OrgRole { Owner = 0, Admin = 1, Member = 2, Guest = 3, Billing = 4 }
