using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Wallets.Queries;

public record GetWalletQuery(Guid UserId) : IRequest<Result<WalletDto>>;

public class GetWalletHandler : IRequestHandler<GetWalletQuery, Result<WalletDto>>
{
    private readonly IPaymentsDbContext _dbContext;

    public GetWalletHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WalletDto>> Handle(GetWalletQuery request, CancellationToken ct)
    {
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, ct);

        if (wallet == null)
            return Result.Failure<WalletDto>(PaymentsErrors.NotFound("Wallet"));

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

public record GetWalletEntriesQuery(Guid WalletId, int Page = 1, int PageSize = 20) : IRequest<Result<WalletEntriesResponse>>;

public record WalletEntriesResponse(
    List<WalletEntryDto> Entries,
    int TotalCount,
    int Page,
    int PageSize);

public class GetWalletEntriesHandler : IRequestHandler<GetWalletEntriesQuery, Result<WalletEntriesResponse>>
{
    private readonly IPaymentsDbContext _dbContext;

    public GetWalletEntriesHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WalletEntriesResponse>> Handle(GetWalletEntriesQuery request, CancellationToken ct)
    {
        var query = _dbContext.WalletEntries
            .Where(e => e.WalletId == request.WalletId)
            .OrderByDescending(e => e.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var entries = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = entries.Select(e => new WalletEntryDto(
            e.Id,
            e.TransactionId,
            e.Credit,
            e.Debit,
            e.BalanceAfter,
            e.Description,
            e.CreatedAt)).ToList();

        return Result.Success(new WalletEntriesResponse(
            dtos,
            totalCount,
            request.Page,
            request.PageSize));
    }
}
