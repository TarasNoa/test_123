using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Payments.Domain.Transactions;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Payment,
    Refund,
    EscrowHold,
    EscrowRelease,
    Fee
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public class Transaction : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? RelatedTaskId { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Transaction() { } // EF Core

    public static Transaction Create(
        Guid id,
        Guid userId,
        TransactionType type,
        decimal amount,
        string currency = "USD",
        string? description = null,
        Guid? relatedTaskId = null,
        string? stripePaymentIntentId = null)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be positive");

        return new Transaction
        {
            Id = id,
            UserId = userId,
            Type = type,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Currency = currency,
            Description = description,
            RelatedTaskId = relatedTaskId,
            StripePaymentIntentId = stripePaymentIntentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(string? stripeChargeId = null)
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be completed");

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        StripeChargeId = stripeChargeId;
    }

    public void Fail()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be marked as failed");

        Status = TransactionStatus.Failed;
    }

    public void Cancel()
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be cancelled");

        Status = TransactionStatus.Cancelled;
    }
}
