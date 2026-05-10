using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain.Chats;

public enum ChatType
{
    Direct,      // Личная переписка 1-on-1
    Group,       // Групповой чат
    TaskRelated  // Чат привязанный к заданию
}

public enum ChatMemberRole
{
    Member,
    Admin,
    Owner
}

public class ChatMember : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public ChatMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    private ChatMember() { } // EF Core

    public ChatMember(Guid id, Guid userId, ChatMemberRole role) : base(id)
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
        Role = ChatMemberRole.Admin;
    }
}

public class Chat : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public ChatType Type { get; private set; }
    public Guid CreatorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<ChatParticipant> Participants { get; private set; } = new();
    public List<Message> Messages { get; private set; } = new();

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

    public void AddParticipant(Guid userId, ChatRole role = ChatRole.Member)
    {
        if (!Participants.Any(p => p.UserId == userId))
        {
            Participants.Add(new ChatParticipant(userId, role));
            RaiseDomainEvent(new ParticipantAddedEvent(Id, userId, role, DateTimeOffset.UtcNow));
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            Participants.Remove(participant);
            RaiseDomainEvent(new ParticipantRemovedEvent(Id, userId, DateTimeOffset.UtcNow));
        }
    }

    public void AddMessage(Message message)
    {
        Messages.Add(message);
        RaiseDomainEvent(new MessageSentEvent(Id, message.Id, message.SenderId, message.Content, message.Timestamp));
    }
}

public enum ChatType { Direct, Group, Channel }
public enum ChatRole { Member, Admin, Owner }

public record ChatParticipant(Guid UserId, ChatRole Role);
