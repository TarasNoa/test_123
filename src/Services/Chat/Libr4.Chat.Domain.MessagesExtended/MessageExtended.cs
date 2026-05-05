using System;
using System.Collections.Generic;

namespace Libr4.Chat.Domain.MessagesExtended;

public enum ReactionType { Like, Love, Laugh, Sad, Angry, Wow }

public class MessageReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class MessageThread
{
    public Guid Id { get; set; }
    public Guid ParentMessageId { get; set; }
    public Guid ChatId { get; set; }
    public List<Guid> ReplyMessageIds { get; set; } = [];
    public int ReplyCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Poll
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<PollOption> Options { get; set; } = [];
    public bool IsAnonymous { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PollOption
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<Guid> VoterIds { get; set; } = [];
    public int VoteCount => VoterIds.Count;
}

public class ScheduledMessage
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset ScheduledFor { get; set; }
    public bool IsSent { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void Send(DateTimeOffset now)
    {
        IsSent = true;
        SentAt = now;
    }
}
