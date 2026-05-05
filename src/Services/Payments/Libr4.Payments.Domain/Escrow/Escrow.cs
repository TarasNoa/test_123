using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Payments.Domain.Escrow;

public enum EscrowStatus
{
    Held,
    Released,
    Refunded,
    Disputed
}

public class Escrow : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public EscrowStatus Status { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    private Escrow() { } // EF Core

    public static Escrow Create(
        Guid id,
        Guid taskId,
        Guid clientId,
        Guid freelancerId,
        decimal amount,
        string currency,
        string stripePaymentIntentId)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be positive");

        return new Escrow
        {
            Id = id,
            TaskId = taskId,
            ClientId = clientId,
            FreelancerId = freelancerId,
            Amount = amount,
            Currency = currency,
            Status = EscrowStatus.Held,
            StripePaymentIntentId = stripePaymentIntentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Release()
    {
        if (Status != EscrowStatus.Held)
            throw new DomainException("Only held escrow can be released");

        Status = EscrowStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        if (Status != EscrowStatus.Held)
            throw new DomainException("Only held escrow can be refunded");

        Status = EscrowStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
    }

    public void Dispute()
    {
        if (Status != EscrowStatus.Held)
            throw new DomainException("Only held escrow can be disputed");

        Status = EscrowStatus.Disputed;
    }
}
