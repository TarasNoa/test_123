using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Trading.Domain.ChartAnalysis.Events;

public record IndicatorValueUpdatedEvent(Guid IndicatorId, string Symbol, decimal NewValue, string? Signal, DateTimeOffset UpdatedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
