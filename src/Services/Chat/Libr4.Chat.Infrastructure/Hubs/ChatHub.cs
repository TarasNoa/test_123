using Microsoft.Extensions.Logging;
using Libr4.Chat.Domain.Messages.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Libr4.Chat.Infrastructure.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);
    Task MessageEdited(Guid messageId, string newContent, DateTime editedAt);
    Task MessageDeleted(Guid messageId);
    Task UserJoined(Guid userId, string userName);
    Task UserLeft(Guid userId);
    Task TypingIndicator(Guid userId, string userName, bool isTyping);
}

public record MessageDto(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string SenderName,
    string Content,
    string Type,
    DateTime SentAt,
    string? FileUrl,
    string? FileName,
    Guid? ReplyToMessageId);

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = Context.User?.Identity?.Name ?? Context.UserIdentifier ?? "anonymous";
        _logger.LogInformation("User {UserId} joining chat {ChatId}", userId, chatId);

        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
        await Clients.Group(chatId.ToString()).UserJoined(Guid.Parse(userId), Context.User?.Identity?.Name ?? "Unknown");
    }

    public async Task LeaveChat(Guid chatId)
    {
        var userId = Context.User?.Identity?.Name ?? Context.UserIdentifier ?? "anonymous";
        _logger.LogInformation("User {UserId} leaving chat {ChatId}", userId, chatId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        await Clients.Group(chatId.ToString()).UserLeft(Guid.Parse(userId));
    }

    public async Task SendTypingIndicator(Guid chatId, bool isTyping)
    {
        var userId = Context.User?.Identity?.Name ?? Context.UserIdentifier ?? "anonymous";
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        await Clients
            .Group(chatId.ToString())
            .TypingIndicator(Guid.Parse(userId), userName, isTyping);
    }

    // Called by server-side event handlers to broadcast messages
    public static async Task BroadcastMessage(IHubContext<ChatHub, IChatClient> hubContext, MessageSent domainEvent)
    {
        var message = new MessageDto(
            domainEvent.MessageId,
            domainEvent.ChatId,
            domainEvent.SenderId,
            "", // User name would be resolved
            domainEvent.Content,
            domainEvent.Type.ToString(),
            domainEvent.SentAt,
            null,
            null,
            null);

        await hubContext.Clients
            .Group(domainEvent.ChatId.ToString())
            .ReceiveMessage(message);
    }
}
