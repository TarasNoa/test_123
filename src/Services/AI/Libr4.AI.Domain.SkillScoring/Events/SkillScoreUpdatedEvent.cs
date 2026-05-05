using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.SkillScoring.Events;

public record SkillScoreUpdatedEvent(Guid SkillScoreId, Guid UserId, string SkillName, float NewScore, string ProficiencyLevel, DateTimeOffset UpdatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
