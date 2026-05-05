using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.LevelUpgrade.Events;

public record LevelUpgradeSuggestedEvent(Guid SuggestionId, Guid UserId, string CurrentLevel, string SuggestedLevel, float ReadinessScore, DateTimeOffset SuggestedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
