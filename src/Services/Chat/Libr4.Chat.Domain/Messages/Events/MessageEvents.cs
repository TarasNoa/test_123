using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Domain.Messages.Events;

public sealed record MessageSent(
    Guid MessageId,
    Guid ChatId,
    Guid SenderId,
    string Content,
    MessageType Type,
    DateTimeOffset SentAt)  : DomainEvent;

public sealed record MessageEdited(
    Guid MessageId,
    Guid ChatId,
    string NewContent,
    DateTimeOffset EditedAt)  : DomainEvent;

public sealed record MessageDeleted(
    Guid MessageId,
    Guid ChatId,
    DateTimeOffset DeletedAt)  : DomainEvent;

public sealed record MessageRead(
    Guid MessageId,
    Guid ChatId,
    Guid UserId,
    DateTimeOffset ReadAt)  : DomainEvent;

public sealed record MessageSentEvent(
    Guid ChatId,
    Guid MessageId,
    Guid SenderId,
    string Content,
    DateTimeOffset Timestamp)  : DomainEvent;
