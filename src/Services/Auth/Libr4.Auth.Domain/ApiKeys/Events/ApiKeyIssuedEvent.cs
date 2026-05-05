using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.ApiKeys.Events;

public record ApiKeyIssuedEvent(Guid ApiKeyId, Guid UserId, string Name, ApiKeyScope Scopes, DateTimeOffset IssuedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => IssuedAt;
}
