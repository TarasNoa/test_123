using System;

namespace Libr4.Auth.Domain.Users;

public class RefreshToken
{
    public string Id { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string TokenHash => Token;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByHash { get; private set; }
    public string? ReplacedByTokenHash => ReplacedByHash;
    public string? IpAddress { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset createdAt, TimeSpan lifetime, string? ipAddress = null)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Token = tokenHash;
        ExpiresAt = createdAt.Add(lifetime);
        CreatedAt = createdAt;
        IpAddress = ipAddress;
        IsRevoked = false;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public void Revoke(DateTimeOffset now, string? ip = null, string? replacedBy = null)
    {
        IsRevoked = true;
        RevokedAt = now;
        IpAddress = ip;
        ReplacedByHash = replacedBy;
    }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsActive() => !IsRevoked && !IsExpired();
    public bool IsActive(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;
}
