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
    DateTime SentAt)  : DomainEvent;

public sealed record MessageEdited(
    Guid MessageId,
    Guid ChatId,
    string NewContent,
    DateTime EditedAt)  : DomainEvent;

public sealed record MessageDeleted(
    Guid MessageId,
    Guid ChatId,
    DateTime DeletedAt)  : DomainEvent;

public sealed record MessageRead(
    Guid MessageId,
    Guid ChatId,
    Guid UserId,
    DateTime ReadAt)  : DomainEvent;
