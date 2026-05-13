using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Wallets.Commands;

public record WithdrawWalletCommand(
    Guid WalletId,
    decimal Amount,
    string Currency,
    string? StripeAccountId) : IRequest<Result<TransactionDto>>;

public class WithdrawWalletValidator : AbstractValidator<WithdrawWalletCommand>
{
    public WithdrawWalletValidator()
    {
        RuleFor(x => x.WalletId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class WithdrawWalletHandler : IRequestHandler<WithdrawWalletCommand, Result<TransactionDto>>
{
    private readonly IPaymentsDbContext _dbContext;

    public WithdrawWalletHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TransactionDto>> Handle(WithdrawWalletCommand request, CancellationToken ct)
    {
        var wallet = await _dbContext.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WalletId, ct);

        if (wallet == null)
            return Result.Failure<TransactionDto>(Error.NotFound("Wallet.NotFound", "Wallet not found"));

        if (wallet.Balance < request.Amount)
            return Result.Failure<TransactionDto>(Error.Conflict("Wallet.InsufficientFunds", "Insufficient balance for withdrawal"));

        // Deduct balance directly via ExecuteUpdateAsync to avoid batching issues
        await _dbContext.Wallets
            .Where(w => w.Id == wallet.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(w => w.Balance, w => w.Balance - request.Amount)
                .SetProperty(w => w.UpdatedAt, w => DateTime.UtcNow), ct);

        // Create withdrawal entry
        var entry = WalletEntry.Create(
            Guid.NewGuid(),
            wallet.Id,
            Guid.NewGuid(),
            0,
            request.Amount,
            wallet.Balance - request.Amount,
            $"Withdrawal to {request.StripeAccountId ?? "bank account"}");

        _dbContext.WalletEntries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new TransactionDto(
            entry.Id,
            wallet.UserId,
            "Withdrawal",
            "Completed",
            request.Amount,
            request.Currency,
            entry.Description,
            entry.CreatedAt,
            entry.CreatedAt);

        return Result.Success(dto);
    }
}
