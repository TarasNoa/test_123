using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Escrow.Commands;
using Libr4.Shared.Contracts.IntegrationEvents.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.IntegrationEvents.Consumers;

public sealed class TaskCompletedConsumer : IConsumer<TaskCompletedIntegrationEvent>
{
    private readonly IPaymentsDbContext _dbContext;
    private readonly ISender _mediator;

    public TaskCompletedConsumer(IPaymentsDbContext dbContext, ISender mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<TaskCompletedIntegrationEvent> context)
    {
        var msg = context.Message;

        var escrow = await _dbContext.Escrows
            .FirstOrDefaultAsync(e => e.TaskId == msg.TaskId, context.CancellationToken);

        if (escrow is null || escrow.Status != Domain.Escrow.EscrowStatus.Held)
            return;

        var command = new ReleaseEscrowCommand(escrow.Id, msg.ClientId);
        await _mediator.Send(command, context.CancellationToken);
    }
}
