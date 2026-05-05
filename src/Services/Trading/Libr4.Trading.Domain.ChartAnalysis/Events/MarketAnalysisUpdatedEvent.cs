using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Trading.Domain.ChartAnalysis.Events;

public record MarketAnalysisUpdatedEvent(Guid AnalysisId, string Symbol, Trend NewTrend, float? BullishScore, float? BearishScore, DateTimeOffset UpdatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
