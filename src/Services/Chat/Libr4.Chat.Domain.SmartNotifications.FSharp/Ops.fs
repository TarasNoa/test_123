namespace Libr4.Chat.Domain.SmartNotifications.FSharp

open System

module SmartNotificationOps =
    let calculatePriority (importance: float) (urgency: float) : Priority =
        let score = importance + urgency
        if score >= 0.8 then Priority.Urgent
        elif score >= 0.6 then Priority.High
        elif score >= 0.4 then Priority.Normal
        else Priority.Low

    let calculateAIScore (keywords: string list) (userHistory: int) : float =
        let keywordScore = float (List.length keywords) / 10.0
        let historyScore = min 1.0 (float userHistory / 100.0)
        (keywordScore + historyScore) / 2.0

    let isImportant (aiScore: float) (priority: Priority) : bool =
        match priority with
        | Priority.Urgent -> true
        | Priority.High -> aiScore > 0.6
        | Priority.Normal -> aiScore > 0.7
        | Priority.Low -> false

module BatchingOps =
    let createBatch (userId: Guid) (notifications: SmartNotification list) (now: DateTimeOffset) : NotificationBatch =
        {
            id = Guid.NewGuid()
            userId = userId
            notifications = notifications
            createdAt = now
            sentAt = None
        }

    let shouldBatch (notifications: SmartNotification list) : bool =
        List.length notifications > 3

    let markAsSent (now: DateTimeOffset) (batch: NotificationBatch) : NotificationBatch =
        { batch with sentAt = Some now }

module DigestOps =
    let createDigest (userId: Guid) (period: string) (notifications: SmartNotification list) (now: DateTimeOffset) : NotificationDigest =
        let summary = sprintf "%d notifications in %s" (List.length notifications) period
        {
            id = Guid.NewGuid()
            userId = userId
            period = period
            notificationCount = List.length notifications
            summary = summary
            createdAt = now
        }
