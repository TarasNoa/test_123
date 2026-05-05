using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Payments.Domain.Transactions.Events;

public record PaymentSucceededDomainEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string? StripeChargeId) : DomainEvent;

public record PaymentFailedDomainEvent(
    Guid TransactionId,
    Guid UserId,
    string Reason) : DomainEvent;

public record RefundIssuedDomainEvent(
    Guid TransactionId,
    Guid OriginalTransactionId,
    Guid UserId,
    decimal Amount,
    string Currency) : DomainEvent;

public record EscrowReleasedDomainEvent(
    Guid EscrowId,
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency) : DomainEvent;
