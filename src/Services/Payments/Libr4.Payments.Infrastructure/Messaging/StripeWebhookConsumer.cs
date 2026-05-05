using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Transactions.Commands;
using Libr4.Payments.Domain.Transactions;
using Libr4.Payments.Domain.Wallets;
using Libr4.Payments.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Stripe;

using StripeEvent = Stripe.Event;

namespace Libr4.Payments.Infrastructure.Messaging;

// This is not a MassTransit consumer - it's called directly from API controller
public class StripeWebhookHandler
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public StripeWebhookHandler(IPaymentsDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task HandleAsync(string json, string stripeSignature, string webhookSecret, CancellationToken ct)
    {
        StripeEvent stripeEvent;
        try
        {
            stripeEvent = ((StripeService)_stripeService).ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid webhook signature: {ex.Message}");
        }

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceeded(stripeEvent, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailed(stripeEvent, ct);
                break;
            case "charge.refunded":
                await HandleChargeRefunded(stripeEvent, ct);
                break;
            default:
                // Unhandled StripeEvent type
                break;
        }
    }

    private async Task HandlePaymentIntentSucceeded(StripeEvent stripeEvent, CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        // Find transaction by PaymentIntent ID
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id, ct);

        if (transaction == null || transaction.Status != TransactionStatus.Pending) return;

        // Complete transaction
        var charge = paymentIntent.LatestChargeId is not null
            ? new Stripe.Charge { Id = paymentIntent.LatestChargeId }
            : null;
        transaction.Complete(charge?.Id);

        // Credit wallet
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet != null)
        {
            wallet.Credit(transaction.Amount, transaction.Id, "Payment received");
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task HandlePaymentIntentFailed(StripeEvent stripeEvent, CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id, ct);

        if (transaction == null || transaction.Status != TransactionStatus.Pending) return;

        transaction.Fail();
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task HandleChargeRefunded(StripeEvent stripeEvent, CancellationToken ct)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        // Find original transaction
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripeChargeId == charge.Id, ct);

        if (transaction == null) return;

        // Create refund transaction
        var refundTransaction = Transaction.Create(
            Guid.NewGuid(),
            transaction.UserId,
            TransactionType.Refund,
            charge.AmountRefunded / 100m, // Convert from cents
            transaction.Currency,
            $"Refund for transaction {transaction.Id}",
            transaction.RelatedTaskId,
            charge.PaymentIntentId);

        refundTransaction.Complete();
        _dbContext.Transactions.Add(refundTransaction);

        // Debit wallet
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet != null)
        {
            wallet.Debit(refundTransaction.Amount, refundTransaction.Id, "Refund issued");
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
