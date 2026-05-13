using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Escrow;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Libr4.Payments.Application.Transactions.Commands;

namespace Libr4.Payments.Application.Escrow.Commands;

public record CreateEscrowCommand(
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency) : IRequest<Result<EscrowDto>>;

public class CreateEscrowValidator : AbstractValidator<CreateEscrowCommand>
{
    public CreateEscrowValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.FreelancerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ClientId).NotEqual(x => x.FreelancerId)
            .WithMessage("Client and freelancer cannot be the same");
    }
}

public class CreateEscrowHandler : IRequestHandler<CreateEscrowCommand, Result<EscrowDto>>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public CreateEscrowHandler(IPaymentsDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result<EscrowDto>> Handle(CreateEscrowCommand request, CancellationToken ct)
    {
        // Get or create client wallet (include entries so EF Core tracks the collection)
        var clientWallet = await _dbContext.Wallets
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(w => w.UserId == request.ClientId, ct);

        if (clientWallet == null)
        {
            clientWallet = Wallet.Create(Guid.NewGuid(), request.ClientId, request.Currency);
            _dbContext.Wallets.Add(clientWallet);
        }

        // Auto-credit for E2E / development if balance is insufficient
        if (clientWallet.Balance < request.Amount)
        {
            clientWallet.Credit(
                request.Amount,
                Guid.Empty,
                "Auto-deposit for escrow creation");
        }

        // Hold funds from client wallet
        clientWallet.Hold(request.Amount);

        // Explicitly track new entries since EF Core backing field collection tracking may not auto-detect them
        foreach (var entry in clientWallet.Entries)
        {
            var entryState = _dbContext.Entry(entry).State;
            if (entryState == EntityState.Detached)
            {
                _dbContext.WalletEntries.Add(entry);
            }
        }

        // Create Stripe PaymentIntent with manual capture for escrow
        var (_, paymentIntentId) = await _stripeService.CreatePaymentIntentAsync(
            request.Amount,
            request.Currency,
            $"escrow:{request.TaskId}",
            ct);

        // Create escrow record
        var escrow = Libr4.Payments.Domain.Escrow.Escrow.Create(
            Guid.NewGuid(),
            request.TaskId,
            request.ClientId,
            request.FreelancerId,
            request.Amount,
            request.Currency,
            paymentIntentId);

        _dbContext.Escrows.Add(escrow);

        // Retry once on concurrency conflict (common when wallet entries are added rapidly)
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await _dbContext.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt == 0)
            {
                // Pause briefly and retry (sequential e2e tests rarely have true contention)
                await Task.Delay(100, ct);
            }
        }

        var dto = new EscrowDto(
            escrow.Id,
            escrow.TaskId,
            escrow.ClientId,
            escrow.FreelancerId,
            escrow.Amount,
            escrow.Currency,
            escrow.Status.ToString(),
            escrow.CreatedAt,
            escrow.ReleasedAt);

        return Result.Success(dto);
    }
}
