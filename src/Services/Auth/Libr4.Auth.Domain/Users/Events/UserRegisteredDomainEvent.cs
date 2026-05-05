using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Users.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string DisplayName) : DomainEvent;
