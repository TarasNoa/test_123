namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public interface IScheduledAgentRunStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ScheduledAgentRunDefinition>> ListAsync(CancellationToken ct = default);

    Task UpsertAsync(ScheduledAgentRunDefinition definition, CancellationToken ct = default);

    Task DeleteAsync(string scheduleId, CancellationToken ct = default);

    Task RecordExecutionAsync(string scheduleId, Guid runId, DateTime executedAtUtc, CancellationToken ct = default);
}

public interface IScheduledAgentRunService
{
    Task EnsureConfiguredSchedulesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ScheduledAgentRunDefinition>> ListAsync(CancellationToken ct = default);

    Task UpsertAsync(ScheduledAgentRunDefinition definition, CancellationToken ct = default);

    Task DeleteAsync(string scheduleId, CancellationToken ct = default);

    Task<IReadOnlyList<ScheduledAgentRunDefinition>> GetDueSchedulesAsync(DateTime utcNow, CancellationToken ct = default);

    Task<ScheduledAgentRunResult> ExecuteAsync(string scheduleId, CancellationToken ct = default);
}

public interface IScheduledAgentRunDispatcher
{
    Task DispatchAsync(ScheduledAgentRunDefinition schedule, CancellationToken ct = default);
}
