using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Domain.Messages.Events;
using Libr4.Chat.Domain.Chats.Events;

namespace Libr4.Chat.Domain.Chats;

public enum ChatType
{
    Direct,      // Личная переписка 1-on-1
    Group,       // Групповой чат
    TaskRelated  // Чат привязанный к заданию
}

public enum ChatRole
{
    Member,
    Admin,
    Owner
}

public class ChatMember : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public ChatRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    private ChatMember() { } // EF Core

    public ChatMember(Guid id, Guid userId, ChatRole role) : base(id)
    {
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        LastReadAt = DateTime.UtcNow;
    }

    public void PromoteToAdmin()
    {
        Role = ChatRole.Admin;
    }
}

public class Chat : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Title => Name;
    public ChatType Type { get; private set; }
    public Guid? RelatedTaskId { get; private set; }
    public Guid CreatorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<ChatMember> Members { get; private set; } = new();
    public List<ChatParticipant> Participants => Members.Select(m => new ChatParticipant(m.UserId, m.Role)).ToList();
    public List<Message> Messages { get; private set; } = new();
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    private Chat() { }

    public static Chat Create(string name, ChatType type, Guid creatorId)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            CreatorId = creatorId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        chat.RaiseDomainEvent(new ChatCreatedEvent(chat.Id, name, type, creatorId, chat.CreatedAt));
        return chat;
    }

    public static Chat CreateGroup(string title, Guid creatorId, Guid? relatedTaskId = null)
    {
        var chat = Create(title, ChatType.Group, creatorId);
        chat.RelatedTaskId = relatedTaskId;
        return chat;
    }

    public static Chat CreateDirect(Guid userId1, Guid userId2)
    {
        var chat = Create("Direct", ChatType.Direct, userId1);
        chat.AddMember(userId2, ChatRole.Member);
        return chat;
    }

    public void AddParticipant(Guid userId, ChatRole role = ChatRole.Member)
    {
        AddMember(userId, role);
    }

    public void AddMember(Guid userId, ChatRole role = ChatRole.Member)
    {
        if (!Members.Any(m => m.UserId == userId))
        {
            Members.Add(new ChatMember(Guid.NewGuid(), userId, role));
            RaiseDomainEvent(new ParticipantAddedEvent(Id, userId, role, DateTimeOffset.UtcNow));
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        RemoveMember(userId);
    }

    public void RemoveMember(Guid userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            Members.Remove(member);
            RaiseDomainEvent(new ParticipantRemovedEvent(Id, userId, DateTimeOffset.UtcNow));
        }
    }

    public void AddMessage(Message message)
    {
        Messages.Add(message);
        RaiseDomainEvent(new MessageSentEvent(Id, message.Id, message.SenderId, message.Content, message.Timestamp));
    }
}

public record ChatParticipant(Guid UserId, ChatRole Role);
