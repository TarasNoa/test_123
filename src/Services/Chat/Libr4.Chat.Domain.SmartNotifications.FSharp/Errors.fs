namespace Libr4.Chat.Domain.SmartNotifications.FSharp

module SmartNotificationErrors =
    type SmartNotificationError =
        | NotificationNotFound
        | BatchingFailed
        | AIScoreCalculationFailed
        | DigestGenerationFailed

    let errorMessage = function
        | NotificationNotFound -> "Notification not found"
        | BatchingFailed -> "Batching failed"
        | AIScoreCalculationFailed -> "AI score calculation failed"
        | DigestGenerationFailed -> "Digest generation failed"

    type ValidationResult<'T> = Result<'T, SmartNotificationError>
