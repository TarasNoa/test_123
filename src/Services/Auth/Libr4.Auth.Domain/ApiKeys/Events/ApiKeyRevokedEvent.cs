using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.ApiKeys.Events;

public record ApiKeyRevokedEvent(Guid ApiKeyId, Guid UserId, string? Reason, DateTimeOffset RevokedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => RevokedAt;
}
