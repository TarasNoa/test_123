using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.Messages.Events;

namespace Libr4.Chat.Domain.Messages;

public class Message : Entity<Guid>
{
    public Guid ChatId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MessageType Type { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public List<MessageAttachment> Attachments { get; private set; } = new();
    public bool IsEdited { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }

    private Message() { }

    public static Message Create(Guid chatId, Guid senderId, string content, MessageType type = MessageType.Text)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            Type = type,
            Timestamp = DateTimeOffset.UtcNow
        };

        return message;
    }

    public void Edit(string newContent)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTimeOffset.UtcNow;
    }

    public void AddAttachment(MessageAttachment attachment)
    {
        Attachments.Add(attachment);
    }
}

public enum MessageType { Text, Image, File, Audio, Video, Code, Call }

public record MessageAttachment(string FileName, string Url, long Size, string ContentType);
