using Libr4.Chat.Domain.Chats;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Domain.Notifications;

namespace Libr4.Chat.Application.Dtos;

public record ChatDto(
    Guid Id,
    string Title,
    ChatType Type,
    Guid? RelatedTaskId,
    DateTime CreatedAt,
    bool IsArchived,
    int MemberCount,
    int UnreadCount,
    MessageDto? LastMessage);

public record ChatMemberDto(
    Guid Id,
    Guid UserId,
    ChatMemberRole Role,
    DateTime JoinedAt,
    DateTime? LastReadAt);

public record MessageDto(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string SenderName,
    string Content,
    MessageType Type,
    MessageStatus Status,
    DateTime SentAt,
    DateTime? EditedAt,
    bool IsDeleted,
    string? FileUrl,
    string? FileName,
    long? FileSize,
    Guid? ReplyToMessageId);

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    NotificationPriority Priority,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    string? ActionUrl,
    string? RelatedEntityId,
    string? RelatedEntityType);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
