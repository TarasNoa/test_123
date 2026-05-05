using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain;
using Libr4.Payments.Domain.Transactions;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.Transactions.Queries;

public record GetTransactionsQuery(
    Guid UserId,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<TransactionsResponse>>;

public record TransactionsResponse(
    List<TransactionDto> Transactions,
    int TotalCount,
    int Page,
    int PageSize);

public class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, Result<TransactionsResponse>>
{
    private readonly IPaymentsDbContext _dbContext;

    public GetTransactionsHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TransactionsResponse>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        var query = _dbContext.Transactions
            .Where(t => t.UserId == request.UserId)
            .AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var transactions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.UserId,
            t.Type.ToString(),
            t.Status.ToString(),
            t.Amount,
            t.Currency,
            t.Description,
            t.CreatedAt,
            t.CompletedAt)).ToList();

        return Result.Success(new TransactionsResponse(
            dtos,
            totalCount,
            request.Page,
            request.PageSize));
    }
}
