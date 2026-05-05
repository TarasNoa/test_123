using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Transactions;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Transactions.Commands;

public record ConfirmPaymentCommand(
    string PaymentIntentId,
    string? ChargeId) : IRequest<Result>;

public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(x => x.PaymentIntentId).NotEmpty();
    }
}

public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, Result>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public ConfirmPaymentHandler(IPaymentsDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result> Handle(ConfirmPaymentCommand request, CancellationToken ct)
    {
        // Find transaction by PaymentIntent ID
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == request.PaymentIntentId, ct);

        if (transaction == null)
            return Result.Failure(PaymentsErrors.NotFound("Transaction"));

        if (transaction.Status != TransactionStatus.Pending)
            return Result.Failure(PaymentsErrors.TransactionAlreadyCompleted);

        // Verify with Stripe
        var confirmed = await _stripeService.ConfirmPaymentIntentAsync(request.PaymentIntentId, ct);
        if (!confirmed)
            return Result.Failure(PaymentsErrors.StripeError("Payment confirmation failed"));

        // Complete transaction
        transaction.Complete(request.ChargeId);

        // Credit wallet
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet != null)
        {
            wallet.Credit(transaction.Amount, transaction.Id, "Payment received");
        }

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
