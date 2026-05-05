using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Escrow;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Libr4.Payments.Application.Transactions.Commands;

namespace Libr4.Payments.Application.Escrow.Commands;

public record RefundEscrowCommand(Guid EscrowId, string Reason) : IRequest<Result<EscrowDto>>;

public class RefundEscrowValidator : AbstractValidator<RefundEscrowCommand>
{
    public RefundEscrowValidator()
    {
        RuleFor(x => x.EscrowId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RefundEscrowHandler : IRequestHandler<RefundEscrowCommand, Result<EscrowDto>>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public RefundEscrowHandler(IPaymentsDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result<EscrowDto>> Handle(RefundEscrowCommand request, CancellationToken ct)
    {
        var escrow = await _dbContext.Escrows
            .FirstOrDefaultAsync(e => e.Id == request.EscrowId, ct);

        if (escrow == null)
            return Result.Failure<EscrowDto>(PaymentsErrors.NotFound("Escrow"));

        if (escrow.Status != EscrowStatus.Held)
            return Result.Failure<EscrowDto>(Error.Conflict("Escrow.NotHeld", "Only held escrow can be refunded"));

        // Get client wallet and release hold back to balance
        var clientWallet = await _dbContext.Wallets
            .FirstAsync(w => w.UserId == escrow.ClientId, ct);

        clientWallet.ReleaseHold(escrow.Amount);

        // Cancel Stripe PaymentIntent if exists
        if (!string.IsNullOrEmpty(escrow.StripePaymentIntentId))
        {
            await _stripeService.CancelPaymentIntentAsync(escrow.StripePaymentIntentId, ct);
        }

        // Refund escrow
        escrow.Refund();
        await _dbContext.SaveChangesAsync(ct);

        var dto = new EscrowDto(
            escrow.Id,
            escrow.TaskId,
            escrow.ClientId,
            escrow.FreelancerId,
            escrow.Amount,
            escrow.Currency,
            escrow.Status.ToString(),
            escrow.CreatedAt,
            null);

        return Result.Success(dto);
    }
}
