using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Sso;

public sealed class ExternalLogin : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public SsoProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; } = "";
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTimeOffset LinkedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    private ExternalLogin() { }

    public static ExternalLogin Link(Guid userId, SsoProvider provider, string providerUserId,
        string? email, string? displayName, string? avatarUrl, DateTimeOffset now)
    {
        return new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            LinkedAt = now,
            LastUsedAt = now
        };
    }

    public void RecordLogin(DateTimeOffset now) => LastUsedAt = now;
}

public enum SsoProvider
{
    Google = 0,
    Microsoft = 1,
    GitHub = 2,
    Okta = 3,
    Facebook = 4,
    Apple = 5,
    Telegram = 6,
    LinkedIn = 7,
    Discord = 8,
    Saml = 99
}
