using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Auth.Domain.Users.Events;

namespace Libr4.Auth.Domain.Users;

public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsEmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public List<RefreshToken> RefreshTokens { get; private set; } = new();

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
}
