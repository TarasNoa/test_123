namespace Libr4.Shared.Contracts.IntegrationEvents.Payments;

public sealed record EscrowReleasedIntegrationEvent(
    Guid EscrowId,
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);

public sealed record PaymentSucceededIntegrationEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);

public sealed record PaymentFailedIntegrationEvent(
    Guid TransactionId,
    Guid UserId,
    string Reason,
    DateTimeOffset OccurredOn);

public sealed record RefundIssuedIntegrationEvent(
    Guid TransactionId,
    Guid OriginalTransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);
