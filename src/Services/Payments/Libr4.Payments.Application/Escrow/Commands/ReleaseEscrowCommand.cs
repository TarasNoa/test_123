using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Escrow;
using Libr4.Payments.Domain.Transactions.Events;
using Libr4.Shared.Contracts.IntegrationEvents.Payments;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Libr4.Payments.Application.Transactions.Commands;

namespace Libr4.Payments.Application.Escrow.Commands;

public record ReleaseEscrowCommand(Guid EscrowId, Guid ReleasedByUserId) : IRequest<Result<EscrowDto>>;

public class ReleaseEscrowValidator : AbstractValidator<ReleaseEscrowCommand>
{
    public ReleaseEscrowValidator()
    {
        RuleFor(x => x.EscrowId).NotEmpty();
    }
}

public class ReleaseEscrowHandler : IRequestHandler<ReleaseEscrowCommand, Result<EscrowDto>>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IEventBus _eventBus;
    private readonly ICurrentUser _currentUser;

    public ReleaseEscrowHandler(
        IPaymentsDbContext dbContext,
        IStripeService stripeService,
        IEventBus eventBus,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _eventBus = eventBus;
        _currentUser = currentUser;
    }

    public async Task<Result<EscrowDto>> Handle(ReleaseEscrowCommand request, CancellationToken ct)
    {
        var escrow = await _dbContext.Escrows
            .FirstOrDefaultAsync(e => e.Id == request.EscrowId, ct);

        if (escrow == null)
            return Result.Failure<EscrowDto>(PaymentsErrors.NotFound("Escrow"));

        // Only client or admin can release escrow
        if (escrow.ClientId != request.ReleasedByUserId && !_currentUser.Roles.Contains("admin"))
        {
            return Result.Failure<EscrowDto>(Error.Forbidden("Escrow.ReleaseForbidden", "Only client or admin can release escrow"));
        }

        if (escrow.Status != EscrowStatus.Held)
            return Result.Failure<EscrowDto>(Error.Conflict("Escrow.NotHeld", "Escrow is not in held status"));

        // Get wallets
        var clientWallet = await _dbContext.Wallets
            .FirstAsync(w => w.UserId == escrow.ClientId, ct);

        var freelancerWallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == escrow.FreelancerId, ct);

        if (freelancerWallet == null)
        {
            // Create freelancer wallet if not exists
            freelancerWallet = Libr4.Payments.Domain.Wallets.Wallet.Create(Guid.NewGuid(), escrow.FreelancerId, escrow.Currency);
            _dbContext.Wallets.Add(freelancerWallet);
        }

        // Release hold and transfer to freelancer
        clientWallet.ReleaseHoldToBeneficiary(
            escrow.Amount,
            freelancerWallet.Id,
            escrow.Id,
            $"Escrow release for task {escrow.TaskId}");

        freelancerWallet.Credit(
            escrow.Amount,
            escrow.Id,
            $"Payment for task {escrow.TaskId}");

        // Capture Stripe payment if exists
        if (!string.IsNullOrEmpty(escrow.StripePaymentIntentId))
        {
            await _stripeService.CapturePaymentIntentAsync(escrow.StripePaymentIntentId, ct);
        }

        // Release escrow
        escrow.Release();

        // Publish integration event for cross-service consumers (e.g. Chat notifications)
        await _eventBus.PublishAsync(new EscrowReleasedIntegrationEvent(
            escrow.Id,
            escrow.TaskId,
            escrow.ClientId,
            escrow.FreelancerId,
            escrow.Amount,
            escrow.Currency,
            DateTimeOffset.UtcNow), ct);

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
            escrow.ReleasedAt);

        return Result.Success(dto);
    }
}

