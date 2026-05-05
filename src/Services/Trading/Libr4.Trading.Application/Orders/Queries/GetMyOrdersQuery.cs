using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Application.Dtos;
using Libr4.Trading.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Application.Orders.Queries;

public record GetMyOrdersQuery(
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<OrderDto>>>;

public class GetMyOrdersHandler : IRequestHandler<GetMyOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly ITradingDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyOrdersHandler(ITradingDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.AssetId,
                o.AssetSymbol,
                o.Type,
                o.Side,
                o.Status,
                o.Quantity,
                o.Price,
                o.StopPrice,
                o.FilledQuantity,
                o.AverageFillPrice,
                o.CreatedAt,
                o.ExecutedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<OrderDto>(orders, totalCount, request.Page, request.PageSize));
    }
}
