using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Trading.Domain.ChartAnalysis.Events;

public record PatternCompletedEvent(Guid PatternId, string Symbol, PatternType PatternType, decimal ActualPrice, DateTimeOffset CompletedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
