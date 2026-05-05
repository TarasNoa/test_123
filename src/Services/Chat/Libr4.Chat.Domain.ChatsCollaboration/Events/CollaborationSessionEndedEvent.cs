using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record CollaborationSessionEndedEvent(Guid SessionId, string SessionIdentifier, Guid InitiatorId, string ContextType, Guid ContextId, DateTimeOffset EndedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
