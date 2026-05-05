using Libr4.Chat.Domain.Notifications;
using Libr4.Chat.Infrastructure.Persistence;
using Libr4.Shared.Contracts.IntegrationEvents.Payments;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Infrastructure.Messaging;

/// <summary>
/// Notifies the freelancer in-app when an escrow is released.
/// </summary>
public sealed class EscrowReleasedConsumer : IConsumer<EscrowReleasedIntegrationEvent>
{
    private readonly ChatDbContext _db;
    private readonly ILogger<EscrowReleasedConsumer> _logger;

    public EscrowReleasedConsumer(ChatDbContext db, ILogger<EscrowReleasedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EscrowReleasedIntegrationEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation(
            "EscrowReleased: freelancer {FreelancerId} received {Amount} {Currency}",
            e.FreelancerId, e.Amount, e.Currency);

        var notification = Notification.ForEscrowReleased(
            e.FreelancerId,
            $"Task {e.TaskId}",
            e.Amount,
            e.TaskId);

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
