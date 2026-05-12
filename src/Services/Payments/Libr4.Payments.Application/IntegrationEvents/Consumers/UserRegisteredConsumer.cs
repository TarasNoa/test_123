using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.IntegrationEvents.Consumers;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly IPaymentsDbContext _dbContext;

    public UserRegisteredConsumer(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var userId = context.Message.UserId;

        var exists = await _dbContext.Wallets.AnyAsync(w => w.UserId == userId, context.CancellationToken);
        if (exists)
            return;

        var wallet = Wallet.Create(Guid.NewGuid(), userId, "USD");
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
