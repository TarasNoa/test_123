using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public sealed class ScheduledAgentRunService : IScheduledAgentRunService
{
    private readonly IScheduledAgentRunStore _store;
    private readonly IAppGenerationRunStarter _starter;
    private readonly IFlowRegistry _flows;
    private readonly AgentSchedulingOptions _options;
    private readonly ILogger<ScheduledAgentRunService> _logger;

    public ScheduledAgentRunService(
        IScheduledAgentRunStore store,
        IAppGenerationRunStarter starter,
        IFlowRegistry flows,
        IOptions<AgentSchedulingOptions> options,
        ILogger<ScheduledAgentRunService> logger)
    {
        _store = store;
        _starter = starter;
        _flows = flows;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureConfiguredSchedulesAsync(CancellationToken ct = default)
    {
        foreach (var entry in _options.Flows.Where(f => f.Enabled && !string.IsNullOrWhiteSpace(f.FlowName)))
        {
            if (!_flows.TryGet(entry.FlowName, out _))
            {
                _logger.LogWarning("Skipping schedule for unknown flow {Flow}", entry.FlowName);
                continue;
            }

            var scheduleId = BuildScheduleId(entry.FlowName);
            var prompt = string.IsNullOrWhiteSpace(entry.Prompt)
                ? $"/flow:{entry.FlowName} scheduled headless run"
                : entry.Prompt!;

            await _store.UpsertAsync(
                    new ScheduledAgentRunDefinition(
                        scheduleId,
                        entry.FlowName,
                        entry.CronExpression,
                        prompt,
                        entry.MaxIterations,
                        entry.Enabled,
                        entry.TenantId),
                    ct)
                .ConfigureAwait(false);
        }
    }

    public Task<IReadOnlyList<ScheduledAgentRunDefinition>> ListAsync(CancellationToken ct = default) =>
        _store.ListAsync(ct);

    public Task UpsertAsync(ScheduledAgentRunDefinition definition, CancellationToken ct = default) =>
        _store.UpsertAsync(definition, ct);

    public Task DeleteAsync(string scheduleId, CancellationToken ct = default) =>
        _store.DeleteAsync(scheduleId, ct);

    public async Task<IReadOnlyList<ScheduledAgentRunDefinition>> GetDueSchedulesAsync(
        DateTime utcNow,
        CancellationToken ct = default)
    {
        var all = await _store.ListAsync(ct).ConfigureAwait(false);
        return all
            .Where(s => s.Enabled && FlowCronParser.IsDue(s.CronExpression, utcNow, s.LastRunAtUtc))
            .ToList();
    }

    public async Task<ScheduledAgentRunResult> ExecuteAsync(string scheduleId, CancellationToken ct = default)
    {
        var schedules = await _store.ListAsync(ct).ConfigureAwait(false);
        var schedule = schedules.FirstOrDefault(s =>
            s.ScheduleId.Equals(scheduleId, StringComparison.OrdinalIgnoreCase));
        if (schedule is null)
            return new ScheduledAgentRunResult(scheduleId, null, false, "schedule_not_found");
        if (!schedule.Enabled)
            return new ScheduledAgentRunResult(scheduleId, null, false, "schedule_disabled");

        var started = await _starter.StartInBackgroundAsync(
                new StartAppGenerationCommand(
                    schedule.UserRequest,
                    MaxIterations: schedule.MaxIterations,
                    TriggerSource: _options.TriggerSource,
                    TriggerActor: "scheduler",
                    TenantId: schedule.TenantId),
                ct)
            .ConfigureAwait(false);

        var runId = started.RunId ?? Guid.NewGuid();
        await _store.RecordExecutionAsync(scheduleId, runId, DateTime.UtcNow, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Scheduled flow {Flow} started schedule={ScheduleId} run={RunId}",
            schedule.FlowName,
            scheduleId,
            runId);

        return new ScheduledAgentRunResult(scheduleId, started.RunId, started.RunId is not null, started.Message);
    }

    public static string BuildScheduleId(string flowName) =>
        $"flow:{flowName}".ToLowerInvariant();
}
