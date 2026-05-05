using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Wallets.Commands;

public record CreateWalletCommand(Guid UserId, string Currency = "USD") : IRequest<Result<WalletDto>>;

public class CreateWalletValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class CreateWalletHandler : IRequestHandler<CreateWalletCommand, Result<WalletDto>>
{
    private readonly IPaymentsDbContext _dbContext;

    public CreateWalletHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WalletDto>> Handle(CreateWalletCommand request, CancellationToken ct)
    {
        // Check if wallet already exists
        var existing = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId && w.Currency == request.Currency, ct);

        if (existing != null)
        {
            var existingDto = new WalletDto(
                existing.Id,
                existing.UserId,
                existing.Balance,
                existing.HeldBalance,
                existing.Currency,
                existing.UpdatedAt);
            return Result.Success(existingDto);
        }

        var wallet = Wallet.Create(Guid.NewGuid(), request.UserId, request.Currency);
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new WalletDto(
            wallet.Id,
            wallet.UserId,
            wallet.Balance,
            wallet.HeldBalance,
            wallet.Currency,
            wallet.UpdatedAt);

        return Result.Success(dto);
    }
}
