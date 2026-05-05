using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Payments.Domain.PaymentMethods.Events;

namespace Libr4.Payments.Domain.PaymentMethods;

public enum PaymentMethodType
{
    Card,
    BankTransfer,
    Wallet
}

public class PaymentMethod : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public PaymentMethodType Type { get; private set; }
    public string? StripePaymentMethodId { get; private set; }
    public string? Last4 { get; private set; }
    public string? Brand { get; private set; }
    public int? ExpMonth { get; private set; }
    public int? ExpYear { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentMethod() { } // EF Core

    public static PaymentMethod CreateCard(
        Guid id,
        Guid userId,
        string stripePaymentMethodId,
        string last4,
        string brand,
        int expMonth,
        int expYear,
        bool isDefault = false)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = id,
            UserId = userId,
            Type = PaymentMethodType.Card,
            StripePaymentMethodId = stripePaymentMethodId,
            Last4 = last4,
            Brand = brand,
            ExpMonth = expMonth,
            ExpYear = expYear,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
        
        paymentMethod.RaiseDomainEvent(new PaymentMethodAddedEvent(paymentMethod.Id, userId, PaymentMethodType.Card, isDefault));
        return paymentMethod;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        RaiseDomainEvent(new PaymentMethodSetAsDefaultEvent(Id, UserId));
    }

    public void RemoveDefault()
    {
        IsDefault = false;
        RaiseDomainEvent(new PaymentMethodRemovedDefaultEvent(Id, UserId));
    }
}
