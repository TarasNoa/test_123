using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Chat.Domain.Messages.Events;

namespace Libr4.Chat.Domain.Messages;

public enum MessageType
{
    Text,
    Image,
    File,
    System  // System notifications (e.g., "User joined the chat")
}

public enum MessageStatus
{
    Sent,
    Delivered,
    Read
}

public class Message : AggregateRoot<Guid>
{
    public Guid ChatId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MessageType Type { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public string? FileUrl { get; private set; }  // For Image/File types
    public string? FileName { get; private set; }
    public long? FileSize { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }

    // ML-powered features
    public float? SentimentScore { get; private set; }  // -1 to 1 (negative to positive)
    public string? SentimentLabel { get; private set; }  // "positive", "negative", "neutral"
    public bool? IsSpam { get; private set; }
    public float? SpamScore { get; private set; }
    public bool? IsConflictDetected { get; private set; }
    public float? ProfessionalToneScore { get; private set; }  // 0 to 1

    private Message() { } // EF Core

    public Message(Guid id, Guid chatId, Guid senderId, string content, 
        MessageType type = MessageType.Text, 
        Guid? replyToMessageId = null,
        string? fileUrl = null,
        string? fileName = null,
        long? fileSize = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(content) && type == MessageType.Text)
            throw new ArgumentException("Content cannot be empty for text messages", nameof(content));

        ChatId = chatId;
        SenderId = senderId;
        Content = content ?? string.Empty;
        Type = type;
        ReplyToMessageId = replyToMessageId;
        FileUrl = fileUrl;
        FileName = fileName;
        FileSize = fileSize;
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new MessageSentEvent(id, chatId, senderId, type, SentAt));
    }

    public void MarkAsDelivered()
    {
        if (Status == MessageStatus.Sent)
        {
            Status = MessageStatus.Delivered;
            RaiseDomainEvent(new MessageDeliveredEvent(Id, ChatId, SenderId));
        }
    }

    public void MarkAsRead()
    {
        Status = MessageStatus.Read;
        RaiseDomainEvent(new MessageReadEvent(Id, ChatId, SenderId));
    }

    public void Edit(string newContent)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot edit deleted message");
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Content cannot be empty", nameof(newContent));

        Content = newContent;
        EditedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MessageEditedEvent(Id, ChatId, SenderId, EditedAt.Value));
    }

    public void SoftDelete()
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            Content = "[deleted]";
            RaiseDomainEvent(new MessageDeletedEvent(Id, ChatId, SenderId));
        }
    }

    public void UpdateFileInfo(string url, string name, long size)
    {
        FileUrl = url;
        FileName = name;
        FileSize = size;
    }

    // ML-powered methods
    public void SetSentimentAnalysis(float score, string label)
    {
        SentimentScore = Math.Clamp(score, -1f, 1f);
        SentimentLabel = label;
        RaiseDomainEvent(new MessageSentimentAnalyzedEvent(Id, score, label));
    }

    public void SetSpamDetection(bool isSpam, float score)
    {
        IsSpam = isSpam;
        SpamScore = Math.Clamp(score, 0f, 1f);
        RaiseDomainEvent(new MessageSpamDetectedEvent(Id, isSpam, score));
    }

    public void SetConflictDetection(bool isConflict)
    {
        IsConflictDetected = isConflict;
        RaiseDomainEvent(new MessageConflictDetectedEvent(Id, isConflict));
    }

    public void SetProfessionalTone(float score)
    {
        ProfessionalToneScore = Math.Clamp(score, 0f, 1f);
        RaiseDomainEvent(new MessageProfessionalToneAnalyzedEvent(Id, score));
    }
}
