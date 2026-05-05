using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Transactions;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Transactions.Commands;

public record CreatePaymentIntentCommand(
    Guid UserId,
    decimal Amount,
    string Currency,
    Guid? TaskId,
    string? Description) : IRequest<Result<PaymentIntentResponse>>;

public record PaymentIntentResponse(
    string ClientSecret,
    string PaymentIntentId,
    Guid TransactionId);

public class CreatePaymentIntentValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be 3-letter code");
    }
}

public class CreatePaymentIntentHandler : IRequestHandler<CreatePaymentIntentCommand, Result<PaymentIntentResponse>>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public CreatePaymentIntentHandler(IPaymentsDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result<PaymentIntentResponse>> Handle(CreatePaymentIntentCommand request, CancellationToken ct)
    {
        // Get or create wallet
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, ct);

        if (wallet == null)
        {
            wallet = Wallet.Create(Guid.NewGuid(), request.UserId, request.Currency);
            _dbContext.Wallets.Add(wallet);
        }

        // Create transaction record
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            request.UserId,
            TransactionType.Payment,
            request.Amount,
            request.Currency,
            request.Description,
            request.TaskId);

        _dbContext.Transactions.Add(transaction);

        // Create Stripe PaymentIntent
        var (clientSecret, paymentIntentId) = await _stripeService.CreatePaymentIntentAsync(
            request.Amount,
            request.Currency,
            transaction.Id.ToString(),
            ct);

        // Update transaction with PaymentIntent ID
        // We need to use reflection or add a method to set this
        // For now, we'll handle this in the entity

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new PaymentIntentResponse(clientSecret, paymentIntentId, transaction.Id));
    }
}

public interface IStripeService
{
    Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string metadata,
        CancellationToken ct);

    Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken ct);

    Task<bool> CapturePaymentIntentAsync(string paymentIntentId, CancellationToken ct);

    Task<bool> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct);

    Task<bool> CreateRefundAsync(string chargeId, CancellationToken ct);
}
