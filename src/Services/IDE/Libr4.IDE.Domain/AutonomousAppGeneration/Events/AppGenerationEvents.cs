using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AutonomousAppGeneration.Events;

public sealed record AppGenerationPlannedEvent(
    Guid OrchestratorId,
    string ApplicationName,
    int PhaseCount,
    int AgentCount) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record AppGenerationStartedEvent(
    Guid OrchestratorId,
    string ApplicationName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record IterationStartedEvent(
    Guid OrchestratorId,
    Guid IterationId,
    int IterationNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record IterationCompletedEvent(
    Guid OrchestratorId,
    Guid IterationId,
    int IterationNumber,
    bool Succeeded,
    int ErrorCount) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record AppGenerationCompletedEvent(
    Guid OrchestratorId,
    string ApplicationName,
    int TotalIterations) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record AppGenerationFailedEvent(
    Guid OrchestratorId,
    string Reason,
    int TotalIterations) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
