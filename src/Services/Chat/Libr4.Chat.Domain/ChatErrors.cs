using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain;

public static class ChatErrors
{
    public static Error ChatNotFound => Error.NotFound(
        "Chat.NotFound",
        "Чат не найден");

    public static Error MessageNotFound => Error.NotFound(
        "Message.NotFound",
        "Сообщение не найдено");

    public static Error NotificationNotFound => Error.NotFound(
        "Notification.NotFound",
        "Уведомление не найдено");

    public static Error UserNotMember => Error.Conflict(
        "Chat.UserNotMember",
        "Пользователь не является участником чата");

    public static Error AlreadyMember => Error.Conflict(
        "Chat.AlreadyMember",
        "Пользователь уже является участником чата");

    public static Error NotOwner => Error.Conflict(
        "Chat.NotOwner",
        "Только владелец может выполнить это действие");

    public static Error ChatArchived => Error.Conflict(
        "Chat.Archived",
        "Чат заархивирован");

    public static Error CannotEditOthersMessage => Error.Conflict(
        "Message.CannotEditOthers",
        "Нельзя редактировать чужое сообщение");

    public static Error CannotDeleteOthersMessage => Error.Conflict(
        "Message.CannotDeleteOthers",
        "Нельзя удалить чужое сообщение");

    public static Error FileTooLarge => Error.Validation(
        "Message.FileTooLarge",
        "Файл слишком большой (макс. 50MB)");
}
