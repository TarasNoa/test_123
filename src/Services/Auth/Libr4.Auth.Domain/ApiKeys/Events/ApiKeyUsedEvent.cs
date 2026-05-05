using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.ApiKeys.Events;

public record ApiKeyUsedEvent(Guid ApiKeyId, Guid UserId, DateTimeOffset UsedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => UsedAt;
}
