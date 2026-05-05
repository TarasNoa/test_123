using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.TaskChat.Events;

namespace Libr4.Tasks.Domain.TaskChat;

public class TaskChat : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public List<ChatMessage> Messages { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }

    private TaskChat() { }

    public void AddMessage(Guid senderId, string senderRole, string content, DateTimeOffset now)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            SenderRole = senderRole, // client, freelancer
            Content = content,
            SentAt = now
        };
        Messages.Add(message);
        LastActivityAt = now;
        RaiseDomainEvent(new ChatMessageAddedEvent(Id, TaskId, senderId, content, now));
    }
}

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}
