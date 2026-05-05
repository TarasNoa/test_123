using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.OrderAssistant.Events;

public record OrderSuggestedEvent(Guid SuggestionId, Guid UserId, string TaskTitle, int SuggestedBudget, int SuggestedDuration, DateTimeOffset SuggestedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
