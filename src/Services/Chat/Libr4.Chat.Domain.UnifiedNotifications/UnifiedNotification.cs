using System;
using System.Collections.Generic;

namespace Libr4.Chat.Domain.UnifiedNotifications;

public enum NotificationStatus { Pending, Sent, Failed, Delivered, Read }
public enum NotificationPriority { Low, Normal, High, Urgent }

public class UnifiedNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, object> Metadata { get; set; } = [];
    public List<NotificationDelivery> Deliveries { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public void MarkAsRead(DateTimeOffset now)
    {
        ReadAt = now;
    }
}

public class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string Channel { get; set; } = string.Empty; // email, push, sms, telegram
    public string Recipient { get; set; } = string.Empty; // email, phone, telegram_id
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public void MarkAsSent(DateTimeOffset now)
    {
        Status = NotificationStatus.Sent;
        SentAt = now;
    }

    public void MarkAsDelivered(DateTimeOffset now)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = now;
    }

    public void MarkAsFailed(string error, DateTimeOffset now)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = error;
    }
}

public class NotificationDigest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<Guid> NotificationIds { get; set; } = [];
    public int Count => NotificationIds.Count;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
