using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Messages.Events;

public record MessageSpamDetectedEvent(Guid MessageId, bool IsSpam, float Score) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
