using Libr4.Chat.Domain.Messages.Events;
using Libr4.Chat.Infrastructure.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Infrastructure.Messaging;

public class MessageSentEventHandler : INotificationHandler<MessageSent>
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<MessageSentEventHandler> _logger;

    public MessageSentEventHandler(
        IHubContext<ChatHub, IChatClient> hubContext,
        ILogger<MessageSentEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(MessageSent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting message {MessageId} to chat {ChatId}",
            notification.MessageId, notification.ChatId);

        await ChatHub.BroadcastMessage(_hubContext, notification);
    }
}

public class MessageEditedEventHandler : INotificationHandler<MessageEdited>
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<MessageEditedEventHandler> _logger;

    public MessageEditedEventHandler(
        IHubContext<ChatHub, IChatClient> hubContext,
        ILogger<MessageEditedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(MessageEdited notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting message edit {MessageId} to chat {ChatId}",
            notification.MessageId, notification.ChatId);

        await _hubContext.Clients
            .Group(notification.ChatId.ToString())
            .MessageEdited(notification.MessageId, notification.NewContent, notification.EditedAt);
    }
}

public class MessageDeletedEventHandler : INotificationHandler<MessageDeleted>
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<MessageDeletedEventHandler> _logger;

    public MessageDeletedEventHandler(
        IHubContext<ChatHub, IChatClient> hubContext,
        ILogger<MessageDeletedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(MessageDeleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting message delete {MessageId} to chat {ChatId}",
            notification.MessageId, notification.ChatId);

        await _hubContext.Clients
            .Group(notification.ChatId.ToString())
            .MessageDeleted(notification.MessageId);
    }
}
