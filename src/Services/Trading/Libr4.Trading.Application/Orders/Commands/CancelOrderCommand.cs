using FluentValidation;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Domain;
using Libr4.Shared.Contracts.IntegrationEvents.Trading;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest<Result>;

public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly ITradingDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public CancelOrderHandler(
        ITradingDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return Result.Failure(TradingErrors.OrderNotFound);

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(TradingErrors.CannotCancelFilledOrder);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new OrderCancelledIntegrationEvent(order.Id, userId, "User requested cancellation", DateTimeOffset.UtcNow),
            cancellationToken);

        return Result.Success();
    }
}
