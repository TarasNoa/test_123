using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Chats;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;
using Libr4.Chat.Domain.Messages;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Application.Chats;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IChatRepository chatRepository, IMessageRepository messageRepository, ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<List<ChatDto>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var chats = await _chatRepository.GetByUserIdAsync(userId, cancellationToken);
        return chats.Select(c => new ChatDto(
            c.Id,
            c.Name,
            c.Type,
            c.CreatorId,
            c.Participants.Select(p => new ChatParticipantDto(p.UserId, p.Role)).ToList(),
            c.CreatedAt)).ToList();
    }

    public async Task<ChatDto> CreateChatAsync(CreateChatRequest request, Guid creatorId, CancellationToken cancellationToken = default)
    {
        var chat = ChatEntity.Create(request.Name, request.Type, creatorId);
        foreach (var participantId in request.ParticipantIds)
        {
            chat.AddParticipant(participantId);
        }
        chat.AddParticipant(creatorId, ChatRole.Owner);

        await _chatRepository.AddAsync(chat, cancellationToken);
        _logger.LogInformation("Chat {ChatId} created by {CreatorId}", chat.Id, creatorId);

        return new ChatDto(chat.Id, chat.Name, chat.Type, chat.CreatorId, 
            chat.Participants.Select(p => new ChatParticipantDto(p.UserId, p.Role)).ToList(), chat.CreatedAt);
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId, CancellationToken cancellationToken = default)
    {
        var chat = await _chatRepository.GetByIdAsync(request.ChatId, cancellationToken);
        if (chat == null) throw new InvalidOperationException("Chat not found");

        if (!chat.Participants.Any(p => p.UserId == senderId))
            throw new UnauthorizedAccessException("User is not a participant");

        var message = Message.Create(request.ChatId, senderId, request.Content, request.Type);
        if (request.Attachments != null)
        {
            foreach (var attachment in request.Attachments)
            {
                message.AddAttachment(new MessageAttachment(attachment.FileName, attachment.Url, attachment.Size, "application/octet-stream"));
            }
        }

        chat.AddMessage(message);
        await _messageRepository.AddAsync(message, cancellationToken);
        await _chatRepository.UpdateAsync(chat, cancellationToken);

        _logger.LogInformation("Message {MessageId} sent in chat {ChatId}", message.Id, request.ChatId);

        return new MessageDto(message.Id, message.ChatId, message.SenderId, message.Content, message.Type, 
            message.Timestamp, message.Attachments.Select(a => new MessageAttachmentDto(a.FileName, a.Url, a.Size)).ToList());
    }

    public async Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.GetByChatIdAsync(chatId, page, pageSize, cancellationToken);
        return messages.Select(m => new MessageDto(m.Id, m.ChatId, m.SenderId, m.Content, m.Type, m.Timestamp,
            m.Attachments.Select(a => new MessageAttachmentDto(a.FileName, a.Url, a.Size)).ToList())).ToList();
    }
}