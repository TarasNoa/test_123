using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.TaskAnalysis.Events;

public record TaskComplexityAnalyzedEvent(Guid AnalysisId, Guid TaskId, int ComplexityScore, string ComplexityLevel, int EstimatedHours, int EstimatedCost, DateTimeOffset AnalyzedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
