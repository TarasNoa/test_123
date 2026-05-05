using Microsoft.Extensions.Logging;
using Libr4.Chat.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Libr4.Chat.Infrastructure.Hubs;

public interface INotificationsClient
{
    Task ReceiveNotification(NotificationDto notification);
    Task NotificationRead(Guid notificationId);
    Task AllNotificationsRead();
    Task UnreadCountUpdated(int count);
}

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string Priority,
    DateTime CreatedAt,
    string? ActionUrl,
    string? RelatedEntityId,
    string? RelatedEntityType);

[Authorize]
public class NotificationsHub : Hub<INotificationsClient>
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} connected to notifications", userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnected from notifications", userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Called by server-side event handlers to push notifications
    public static async Task PushNotification(
        IHubContext<NotificationsHub, INotificationsClient> hubContext,
        Notification notification)
    {
        var dto = new NotificationDto(
            notification.Id,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.Priority.ToString(),
            notification.CreatedAt,
            notification.ActionUrl,
            notification.RelatedEntityId,
            notification.RelatedEntityType);

        await hubContext.Clients
            .Group($"user_{notification.UserId}")
            .ReceiveNotification(dto);
    }

    public static async Task UpdateUnreadCount(
        IHubContext<NotificationsHub, INotificationsClient> hubContext,
        Guid userId,
        int count)
    {
        await hubContext.Clients
            .Group($"user_{userId}")
            .UnreadCountUpdated(count);
    }
}
