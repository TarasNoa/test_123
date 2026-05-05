using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Payments.Domain;

public static class PaymentsErrors
{
    public static Error NotFound(string entity) =>
        Error.NotFound($"{entity}.NotFound", $"{entity} not found");

    public static Error InsufficientBalance =>
        Error.Conflict("Wallet.InsufficientBalance", "Insufficient balance for operation");

    public static Error InvalidAmount =>
        Error.Validation("Payment.InvalidAmount", "Amount must be greater than zero");

    public static Error EscrowAlreadyReleased =>
        Error.Conflict("Escrow.AlreadyReleased", "Escrow has already been released");

    public static Error EscrowAlreadyRefunded =>
        Error.Conflict("Escrow.AlreadyRefunded", "Escrow has already been refunded");

    public static Error TransactionAlreadyCompleted =>
        Error.Conflict("Transaction.AlreadyCompleted", "Transaction has already been completed");

    public static Error StripeError(string message) =>
        Error.Failure("Stripe.Error", message);
}
