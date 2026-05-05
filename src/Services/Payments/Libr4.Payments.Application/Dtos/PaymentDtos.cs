namespace Libr4.Payments.Application.Dtos;

public record TransactionDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Status,
    decimal Amount,
    string Currency,
    string? Description,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record WalletDto(
    Guid Id,
    Guid UserId,
    decimal Balance,
    decimal HeldBalance,
    string Currency,
    DateTime UpdatedAt);

public record WalletEntryDto(
    Guid Id,
    Guid TransactionId,
    decimal Credit,
    decimal Debit,
    decimal BalanceAfter,
    string Description,
    DateTime CreatedAt);

public record EscrowDto(
    Guid Id,
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? ReleasedAt);

public record PaymentMethodDto(
    Guid Id,
    string Type,
    string? Last4,
    string? Brand,
    int? ExpMonth,
    int? ExpYear,
    bool IsDefault);

// Requests
public record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    Guid? TaskId,
    string? Description);

public record ConfirmPaymentRequest(
    string PaymentIntentId);

public record CreateEscrowRequest(
    Guid TaskId,
    Guid FreelancerId,
    decimal Amount,
    string Currency);

public record ReleaseEscrowRequest(
    Guid EscrowId);

public record AddPaymentMethodRequest(
    string StripePaymentMethodId,
    bool SetAsDefault = false);

public record WithdrawRequest(
    decimal Amount,
    string? StripeAccountId);
