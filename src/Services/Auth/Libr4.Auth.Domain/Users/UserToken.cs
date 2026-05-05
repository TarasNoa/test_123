namespace Libr4.Auth.Domain.Users;

public enum UserTokenKind
{
    EmailConfirmation = 0,
    PasswordReset = 1,
}

public sealed class UserToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public UserTokenKind Kind { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private UserToken() { }

    public UserToken(Guid userId, UserTokenKind kind, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Kind = kind;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = now.Add(lifetime);
    }

    public bool IsActive(DateTimeOffset now) => ConsumedAt is null && ExpiresAt > now;

    public void Consume(DateTimeOffset now) => ConsumedAt = now;
}
