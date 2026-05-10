using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Collaboration.Domain;

public class ChatMessage : Entity<Guid>
{
    public Guid RoomId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MessageType Type { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public List<ChatAttachment> Attachments { get; private set; } = new();
    public bool IsEdited { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public Guid? ReplyToId { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(Guid roomId, Guid senderId, string content, MessageType type = MessageType.Text)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            SenderId = senderId,
            Content = content,
            Type = type,
            SentAt = DateTimeOffset.UtcNow
        };
    }

    public void Edit(string newContent)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTimeOffset.UtcNow;
    }

    public void AddAttachment(ChatAttachment attachment)
    {
        Attachments.Add(attachment);
    }

    public void SetReplyToId(Guid replyToId)
    {
        ReplyToId = replyToId;
    }
}

public enum MessageType { Text, Code, File, Image, Voice }

public record ChatAttachment(string FileName, string Url, long Size, string ContentType);
