using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Users.Events;

public sealed record UserCreatedEvent(Guid UserId, string Email, string Username, DateTimeOffset CreatedAt) : DomainEvent;

public sealed record UserEmailVerifiedEvent(Guid UserId, DateTimeOffset VerifiedAt) : DomainEvent;

public sealed record UserLoggedInEvent(Guid UserId, DateTimeOffset LoggedInAt) : DomainEvent;

public sealed record UserRoleAddedEvent(Guid UserId, string RoleName, DateTimeOffset AddedAt) : DomainEvent;

public sealed record RefreshTokenAddedEvent(Guid UserId, string TokenId, DateTimeOffset AddedAt) : DomainEvent;

public sealed record RefreshTokenRevokedEvent(Guid UserId, string TokenId, DateTimeOffset RevokedAt) : DomainEvent;
