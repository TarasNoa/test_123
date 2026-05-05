using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain.Chats.Events;

public sealed record ChatCreated(Guid ChatId, Guid CreatorId, string Title, ChatType Type)  : DomainEvent;
public sealed record MemberJoined(Guid ChatId, Guid UserId, ChatMemberRole Role)  : DomainEvent;
public sealed record MemberLeft(Guid ChatId, Guid UserId)  : DomainEvent;
public sealed record ChatArchived(Guid ChatId, DateTime ArchivedAt)  : DomainEvent;
