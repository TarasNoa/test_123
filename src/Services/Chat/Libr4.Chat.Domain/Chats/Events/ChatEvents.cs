using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain.Chats.Events;

public sealed record ChatCreatedEvent(Guid ChatId, string Title, ChatType Type, Guid CreatorId, DateTimeOffset CreatedAt) : DomainEvent;
public sealed record ChatCreated(Guid ChatId, Guid CreatorId, string Title, ChatType Type);
public sealed record ParticipantAddedEvent(Guid ChatId, Guid UserId, ChatRole Role, DateTimeOffset AddedAt) : DomainEvent;
public sealed record ParticipantRemovedEvent(Guid ChatId, Guid UserId, DateTimeOffset RemovedAt) : DomainEvent;
public sealed record ChatArchived(Guid ChatId, DateTime ArchivedAt)  : DomainEvent;
public sealed record MemberJoined(Guid ChatId, Guid UserId, ChatRole Role)  : DomainEvent;
public sealed record MemberLeft(Guid ChatId, Guid UserId)  : DomainEvent;
