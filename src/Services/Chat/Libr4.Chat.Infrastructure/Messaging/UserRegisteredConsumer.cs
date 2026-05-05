using Libr4.Chat.Infrastructure.Persistence;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Infrastructure.Messaging;

/// <summary>
/// Creates a notification record for a new user so the Chat service
/// can fan-out welcome messages without querying Auth.
/// </summary>
public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly ChatDbContext _db;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ChatDbContext db, ILogger<UserRegisteredConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("Chat: new user registered {UserId} ({Email})", e.UserId, e.Email);

        var alreadyExists = await _db.Notifications
            .AnyAsync(n => n.UserId == e.UserId, context.CancellationToken);

        if (alreadyExists)
            return;

        var notification = new Libr4.Chat.Domain.Notifications.Notification(
            Guid.NewGuid(),
            e.UserId,
            Libr4.Chat.Domain.Notifications.NotificationType.System,
            "Welcome to Libr4",
            $"Welcome, {e.DisplayName}! Your account is ready.");

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
