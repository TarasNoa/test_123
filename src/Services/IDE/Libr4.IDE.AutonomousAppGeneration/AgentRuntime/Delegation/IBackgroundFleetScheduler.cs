namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public interface IBackgroundFleetScheduler
{
    Task ScheduleAsync(
        BackgroundDelegationRequest request,
        Func<CancellationToken, Task> executeAsync,
        CancellationToken ct = default);

    Task<BackgroundFleetSummary> GetSummaryAsync(BackgroundFleetListQuery query, CancellationToken ct = default);

    void RaiseImplementerBudgetPressure(Guid runId, string? tenantUserId = null);
}
