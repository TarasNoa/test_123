using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.RealtimeCollaboration.Events;

public record DocumentOperationAppliedEvent(Guid DocumentId, string DocumentName, OperationType OperationType, Guid UserId, int Version, DateTimeOffset AppliedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
