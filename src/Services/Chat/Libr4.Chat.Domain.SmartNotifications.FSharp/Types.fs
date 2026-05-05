namespace Libr4.Chat.Domain.SmartNotifications.FSharp

open System

type Priority = Low | Normal | High | Urgent
type NotificationAction = Read | Archive | Snooze | Delete

type SmartNotification = {
    id: Guid
    userId: Guid
    title: string
    message: string
    priority: Priority
    aiScore: float
    isImportant: bool
    createdAt: DateTimeOffset
    readAt: DateTimeOffset option
}

type NotificationBatch = {
    id: Guid
    userId: Guid
    notifications: SmartNotification list
    createdAt: DateTimeOffset
    sentAt: DateTimeOffset option
}

type NotificationDigest = {
    id: Guid
    userId: Guid
    period: string
    notificationCount: int
    summary: string
    createdAt: DateTimeOffset
}
