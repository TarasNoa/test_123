using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.TaskRecommendations.Events;

public record TaskRecommendedEvent(Guid RecommendationId, Guid UserId, Guid TaskId, float MatchScore, DateTimeOffset RecommendedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
