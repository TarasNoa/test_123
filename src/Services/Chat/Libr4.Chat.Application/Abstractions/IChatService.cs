using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Chat.Domain.Chats;
using Libr4.Chat.Domain.Messages;

namespace Libr4.Chat.Application.Abstractions;

public record ChatDto(
    Guid Id,
    string Name,
    ChatType Type,
    Guid CreatorId,
    List<ChatParticipantDto> Participants,
    DateTimeOffset CreatedAt);

public record ChatParticipantDto(Guid UserId, ChatRole Role);

public record MessageDto(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string Content,
    MessageType Type,
    DateTimeOffset Timestamp,
    List<MessageAttachmentDto> Attachments);

public record MessageAttachmentDto(string FileName, string Url, long Size);

public record CreateChatRequest(string Name, ChatType Type, List<Guid> ParticipantIds);
public record SendMessageRequest(Guid ChatId, string Content, MessageType Type, List<MessageAttachmentDto>? Attachments);

public interface IChatService
{
    Task<List<ChatDto>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatDto> CreateChatAsync(CreateChatRequest request, Guid creatorId, CancellationToken cancellationToken = default);
    Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId, CancellationToken cancellationToken = default);
    Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}