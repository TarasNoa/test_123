using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Users.Events;

public sealed record EmailConfirmationRequestedDomainEvent(Guid UserId, string Email, string Token) : DomainEvent;

public sealed record PasswordResetRequestedDomainEvent(Guid UserId, string Email, string Token) : DomainEvent;

public sealed record EmailConfirmedDomainEvent(Guid UserId, string Email) : DomainEvent;

public sealed record PasswordChangedDomainEvent(Guid UserId) : DomainEvent;
