using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Social.Domain.CommunityStats.Events;

public record CommunityStatsUpdatedEvent(Guid StatsId, string CommunityName, int TotalMembers, int ActiveMembers, DateTimeOffset UpdatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
