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
    public string Title { get; private set; } = string.Empty;
    public ChatType Type { get; private set; }
    public Guid? RelatedTaskId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public bool IsArchived => ArchivedAt.HasValue;

    private readonly List<ChatMember> _members = new();
    public IReadOnlyCollection<ChatMember> Members => _members.AsReadOnly();

    private Chat() { } // EF Core

    public Chat(Guid id, string title, ChatType type, Guid? relatedTaskId = null) : base(id)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Type = type;
        RelatedTaskId = relatedTaskId;
        CreatedAt = DateTime.UtcNow;
    }

    public static Chat CreateDirect(Guid userA, Guid userB)
    {
        var chat = new Chat(Guid.NewGuid(), "Direct", ChatType.Direct);
        chat.AddMember(userA, ChatMemberRole.Owner);
        chat.AddMember(userB, ChatMemberRole.Owner);
        return chat;
    }

    public static Chat CreateGroup(string title, Guid creatorId, Guid? relatedTaskId = null)
    {
        var chat = new Chat(Guid.NewGuid(), title, relatedTaskId.HasValue ? ChatType.TaskRelated : ChatType.Group, relatedTaskId);
        chat.AddMember(creatorId, ChatMemberRole.Owner);
        return chat;
    }

    public void AddMember(Guid userId, ChatMemberRole role = ChatMemberRole.Member)
    {
        if (_members.Any(m => m.UserId == userId))
            return;

        _members.Add(new ChatMember(Guid.NewGuid(), userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
            _members.Remove(member);
    }

    public void Archive()
    {
        if (!IsArchived)
            ArchivedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        Title = title;
    }
}
