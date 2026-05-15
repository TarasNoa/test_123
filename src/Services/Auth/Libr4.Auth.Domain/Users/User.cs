using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Auth.Domain.Users.Events;

namespace Libr4.Auth.Domain.Users;

public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string DisplayName => Username;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsEmailVerified { get; private set; }
    public bool EmailConfirmed => IsEmailVerified;
    public bool IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecretEncrypted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public List<RefreshToken> RefreshTokens { get; private set; } = new();
    public List<UserToken> Tokens { get; private set; } = new();
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public DateTimeOffset? LockedOutUntil => LockoutEnd;

    // Role flags
    public bool IsFreelancer { get; private set; }
    public bool IsClient { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsDeveloper { get; private set; }
    public bool IsTrader { get; private set; }
    public bool IsLearner { get; private set; }
    public bool IsSocialOnly { get; private set; }

    // Profile fields
    public string? FullName { get; private set; }
    public string? Bio { get; private set; }
    public List<string> Skills { get; private set; } = new();
    public decimal? HourlyRate { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CoverUrl { get; private set; }

    // Extended profile
    public string? Role { get; private set; }
    public string? Phone { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? CompanyName { get; private set; }
    public string? Industry { get; private set; }
    public string? CompanySize { get; private set; }
    public string? Website { get; private set; }
    public string? Experience { get; private set; }
    public string? Specialization { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? CvUrl { get; private set; }

    // Stats
    public decimal? Rating { get; private set; }
    public decimal? TotalEarnings { get; private set; }
    public decimal? TotalSpent { get; private set; }
    public int CompletedTasks { get; private set; }

    // KYC/AML
    public bool KycVerified { get; private set; }
    public string? KycStatus { get; private set; }
    public bool AmlChecked { get; private set; }
    public bool SanctionsChecked { get; private set; }

    // AI matching
    public int? Level { get; private set; }
    public int? SkillScore { get; private set; }

    private User() { }

    public static User Create(string email, string username, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email, username, user.CreatedAt));
        return user;
    }

    public static User Register(
        string email, string displayName, string passwordHash, DateTimeOffset now,
        string? role = null, string? phone = null, string? country = null, string? city = null,
        string? companyName = null, string? industry = null, string? companySize = null,
        string? website = null, List<string>? skills = null, string? experience = null,
        decimal? hourlyRate = null, string? specialization = null, string? linkedInUrl = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = displayName,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = now,
            Role = role,
            Phone = phone,
            Country = country,
            City = city,
            CompanyName = companyName,
            Industry = industry,
            CompanySize = companySize,
            Website = website,
            Skills = skills ?? new(),
            Experience = experience,
            HourlyRate = hourlyRate,
            Specialization = specialization,
            LinkedInUrl = linkedInUrl
        };

        if (role == "freelancer") user.IsFreelancer = true;
        if (role == "client") user.IsClient = true;

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email, displayName, now));
        return user;
    }

    public void VerifyEmail()
    {
        if (!IsEmailVerified)
        {
            IsEmailVerified = true;
            RaiseDomainEvent(new UserEmailVerifiedEvent(Id, DateTimeOffset.UtcNow));
        }
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new UserLoggedInEvent(Id, LastLoginAt.Value));
    }

    public void RecordFailedLogin(DateTimeOffset now)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockoutEnd = now.AddMinutes(15);
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdateLastLogin();
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd.HasValue && LockoutEnd.Value > now;

    public void AddRole(Role role)
    {
        if (!Roles.Contains(role))
        {
            Roles.Add(role);
            RaiseDomainEvent(new UserRoleAddedEvent(Id, role.Name, DateTimeOffset.UtcNow));
        }
    }

    public void AddRefreshToken(RefreshToken token)
    {
        RefreshTokens.Add(token);
        RaiseDomainEvent(new RefreshTokenAddedEvent(Id, token.Id, DateTimeOffset.UtcNow));
    }

    public void RevokeRefreshToken(string tokenId)
    {
        var token = RefreshTokens.FirstOrDefault(t => t.Id == tokenId);
        if (token != null)
        {
            token.Revoke();
            RaiseDomainEvent(new RefreshTokenRevokedEvent(Id, tokenId, DateTimeOffset.UtcNow));
        }
    }

    public void EnableTwoFactor(string encryptedSecret)
    {
        TwoFactorEnabled = true;
        TwoFactorSecretEncrypted = encryptedSecret;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecretEncrypted = null;
    }

    public void IssueToken(UserTokenKind kind, string hash, DateTimeOffset now, TimeSpan lifetime)
    {
        Tokens.Add(new UserToken(Id, kind, hash, now, lifetime));
    }

    public bool ConfirmEmail(string hash, DateTimeOffset now)
    {
        var token = Tokens.FirstOrDefault(t => t.TokenHash == hash && t.Kind == UserTokenKind.EmailConfirmation);
        if (token is null || !token.IsActive(now))
            return false;

        token.Consume(now);
        VerifyEmail();
        return true;
    }

    public bool ResetPassword(string tokenHash, string newPasswordHash, DateTimeOffset now)
    {
        var token = Tokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.Kind == UserTokenKind.PasswordReset);
        if (token is null || !token.IsActive(now))
            return false;

        token.Consume(now);
        PasswordHash = newPasswordHash;
        // Revoke all existing refresh tokens on password reset
        foreach (var rt in RefreshTokens.Where(r => r.IsActive()))
            rt.Revoke();
        return true;
    }

    public void UpdateCoverUrl(string? url)
    {
        CoverUrl = url;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
