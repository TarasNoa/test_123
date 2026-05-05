using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.PaymentMethods.Events;

public record PaymentMethodRemovedDefaultEvent(Guid PaymentMethodId, Guid UserId) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
