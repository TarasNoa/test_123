using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.PaymentMethods.Events;

public record PaymentMethodAddedEvent(Guid PaymentMethodId, Guid UserId, PaymentMethodType Type, bool IsDefault) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
