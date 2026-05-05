using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain.Notifications;

public enum NotificationType
{
    MessageReceived,
    TaskUpdate,
    PaymentReceived,
    EscrowReleased,
    ApplicationAccepted,
    ReviewReceived,
    System
}

public enum NotificationPriority
{
    Low,
    Normal,
    High
}

public class Notification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationPriority Priority { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ActionUrl { get; private set; }  // Deep link to relevant page
    public string? RelatedEntityId { get; private set; }  // TaskId, ChatId, etc.
    public string? RelatedEntityType { get; private set; }

    private Notification() { } // EF Core

    public Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        string? actionUrl = null,
        string? relatedEntityId = null,
        string? relatedEntityType = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        Priority = priority;
        ActionUrl = actionUrl;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public static Notification ForMessage(Guid userId, string senderName, string chatTitle, Guid chatId)
    {
        return new Notification(
            Guid.NewGuid(),
            userId,
            NotificationType.MessageReceived,
            $"Новое сообщение от {senderName}",
            $"В чате \"{chatTitle}\"",
            NotificationPriority.Normal,
            $"/chats/{chatId}",
            chatId.ToString(),
            "Chat"
        );
    }

    public static Notification ForTaskApplicationAccepted(Guid userId, string taskTitle, Guid taskId)
    {
        return new Notification(
            Guid.NewGuid(),
            userId,
            NotificationType.ApplicationAccepted,
            "Заявка принята",
            $"Ваша заявка на задание \"{taskTitle}\" принята",
            NotificationPriority.High,
            $"/tasks/{taskId}",
            taskId.ToString(),
            "Task"
        );
    }

    public static Notification ForEscrowReleased(Guid userId, string taskTitle, decimal amount, Guid taskId)
    {
        return new Notification(
            Guid.NewGuid(),
            userId,
            NotificationType.EscrowReleased,
            "Средства получены",
            $"${amount} за \"{taskTitle}\" переведены на ваш кошелек",
            NotificationPriority.High,
            $"/wallet",
            taskId.ToString(),
            "Task"
        );
    }
}
