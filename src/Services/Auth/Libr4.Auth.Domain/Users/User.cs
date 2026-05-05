using Libr4.Auth.Domain.Users.Events;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _roles = new();
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<UserToken> _tokens = new();

    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecretEncrypted { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedOutUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    // Role flags (from Python User model)
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
    public decimal HourlyRate { get; private set; }
    public string? AvatarUrl { get; private set; }
    public List<UserLanguage> Languages { get; private set; } = new();

    // Stats
    public float Rating { get; private set; }
    public decimal TotalEarnings { get; private set; }
    public decimal TotalSpent { get; private set; }
    public int CompletedTasks { get; private set; }

    // KYC/AML
    public bool KycVerified { get; private set; }
    public string KycStatus { get; private set; } = "pending";
    public bool AmlChecked { get; private set; }
    public bool SanctionsChecked { get; private set; }

    // AI matching
    public int Level { get; private set; }
    public float SkillScore { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<UserToken> Tokens => _tokens.AsReadOnly();

    private User() { }

    public static User Register(string email, string displayName, string passwordHash, DateTimeOffset now)
    {
        var u = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now,
        };
        u._roles.Add(new UserRole(u.Id, Role.User));
        u.RaiseDomainEvent(new UserRegisteredDomainEvent(u.Id, u.Email, u.DisplayName));
        return u;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void RecordFailedLogin(DateTimeOffset now, int maxAttempts = 5, TimeSpan? lockoutDuration = null)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
            LockedOutUntil = now.Add(lockoutDuration ?? TimeSpan.FromMinutes(15));
        UpdatedAt = now;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedOutUntil.HasValue && LockedOutUntil.Value > now;

    public void AddRole(Role role)
    {
        if (_roles.Any(r => r.Role == role)) return;
        _roles.Add(new UserRole(Id, role));
    }

    public void RemoveRole(Role role) => _roles.RemoveAll(r => r.Role == role);

    // Role flag management
    public void SetFreelancer(bool isFreelancer)
    {
        IsFreelancer = isFreelancer;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetClient(bool isClient)
    {
        IsClient = isClient;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAdmin(bool isAdmin)
    {
        IsAdmin = isAdmin;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDeveloper(bool isDeveloper)
    {
        IsDeveloper = isDeveloper;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetTrader(bool isTrader)
    {
        IsTrader = isTrader;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLearner(bool isLearner)
    {
        IsLearner = isLearner;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSocialOnly(bool isSocialOnly)
    {
        IsSocialOnly = isSocialOnly;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Profile management
    public void UpdateProfile(string? fullName, string? bio, List<string>? skills, decimal? hourlyRate, string? avatarUrl)
    {
        if (fullName != null) FullName = fullName;
        if (bio != null) Bio = bio;
        if (skills != null) Skills = skills;
        if (hourlyRate.HasValue && hourlyRate.Value >= 0) HourlyRate = hourlyRate.Value;
        if (avatarUrl != null) AvatarUrl = avatarUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLanguages(List<UserLanguage> languages)
    {
        Languages = languages;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Stats management
    public void UpdateStats(float rating, decimal totalEarnings, decimal totalSpent, int completedTasks)
    {
        Rating = rating;
        TotalEarnings = totalEarnings;
        TotalSpent = totalSpent;
        CompletedTasks = completedTasks;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // KYC/AML management
    public void SetKycVerified(bool verified, string status = "approved")
    {
        KycVerified = verified;
        KycStatus = verified ? status : "pending";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAmlChecked(bool isChecked)
    {
        AmlChecked = isChecked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSanctionsChecked(bool isChecked)
    {
        SanctionsChecked = isChecked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // AI matching management
    public void SetLevel(int level)
    {
        if (level < 1 || level > 10) throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 10");
        Level = level;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSkillScore(float score)
    {
        if (score < 0.0f || score > 10.0f) throw new ArgumentOutOfRangeException(nameof(score), "Skill score must be between 0.0 and 10.0");
        SkillScore = score;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnableTwoFactor(string encryptedSecret)
    {
        TwoFactorSecretEncrypted = encryptedSecret;
        TwoFactorEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecretEncrypted = null;
        TwoFactorEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddRefreshToken(RefreshToken token) => _refreshTokens.Add(token);

    public UserToken IssueToken(UserTokenKind kind, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        // Invalidate any prior pending tokens of the same kind
        foreach (var t in _tokens.Where(t => t.Kind == kind && t.ConsumedAt is null))
            t.Consume(now);

        var token = new UserToken(Id, kind, tokenHash, now, lifetime);
        _tokens.Add(token);
        return token;
    }

    public bool ConfirmEmail(string tokenHash, DateTimeOffset now)
    {
        var token = _tokens.FirstOrDefault(t =>
            t.Kind == UserTokenKind.EmailConfirmation && t.TokenHash == tokenHash && t.IsActive(now));
        if (token is null) return false;
        token.Consume(now);
        EmailConfirmed = true;
        UpdatedAt = now;
        return true;
    }

    public bool ResetPassword(string tokenHash, string newPasswordHash, DateTimeOffset now)
    {
        var token = _tokens.FirstOrDefault(t =>
            t.Kind == UserTokenKind.PasswordReset && t.TokenHash == tokenHash && t.IsActive(now));
        if (token is null) return false;
        token.Consume(now);
        PasswordHash = newPasswordHash;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
        UpdatedAt = now;
        // invalidate all refresh tokens
        foreach (var rt in _refreshTokens.Where(r => r.RevokedAt is null))
            rt.Revoke(now, null);
        return true;
    }
}

// Language proficiency for user
public record UserLanguage(string Code, string Proficiency); // Proficiency: Native, Fluent, Conversational, Basic
