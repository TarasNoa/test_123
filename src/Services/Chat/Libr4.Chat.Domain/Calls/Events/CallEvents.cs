using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Calls.Events;

public sealed record CallInitiatedEvent(Guid CallId, Guid ChatId, Guid InitiatorId, CallType Type, DateTimeOffset StartedAt) : DomainEvent;

public sealed record CallEndedEvent(Guid CallId, DateTimeOffset EndedAt) : DomainEvent;
