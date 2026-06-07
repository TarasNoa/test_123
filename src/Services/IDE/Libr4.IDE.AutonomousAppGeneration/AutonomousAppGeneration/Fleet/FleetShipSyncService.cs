using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetShipSyncService
{
    Task RecordShipResultAsync(Guid runId, GitHubShipResult result, CancellationToken ct = default);
    Task ApplyCiWebhookAsync(GitHubCiWebhookPayload payload, CancellationToken ct = default);
    AgentFleetStatus? ResolveStatus(AppGenerationOrchestrator? run, RunShipState? ship);
}

public sealed class FleetShipSyncService : IFleetShipSyncService
{
    private readonly IFleetShipStateStore _store;
    private readonly Lazy<IAgentFleetRegistry> _fleet;
    private readonly ICiRepairDispatcher? _ciRepair;
    private readonly CiRepairOptions _ciRepairOptions;
    private readonly ILogger<FleetShipSyncService> _logger;

    public FleetShipSyncService(
        IFleetShipStateStore store,
        Lazy<IAgentFleetRegistry> fleet,
        ILogger<FleetShipSyncService> logger,
        ICiRepairDispatcher? ciRepair = null,
        Microsoft.Extensions.Options.IOptions<CiRepairOptions>? ciRepairOptions = null)
    {
        _store = store;
        _fleet = fleet;
        _logger = logger;
        _ciRepair = ciRepair;
        _ciRepairOptions = ciRepairOptions?.Value ?? new CiRepairOptions();
    }

    public async Task RecordShipResultAsync(Guid runId, GitHubShipResult result, CancellationToken ct = default)
    {
        if (result.Skipped || !result.Success)
            return;

        var existing = await _store.GetAsync(runId, ct).ConfigureAwait(false);
        var ciStatus = result.WorkflowRunId is not null || !string.IsNullOrWhiteSpace(result.HeadBranch)
            ? FleetCiStatus.Pending
            : FleetCiStatus.None;

        var state = new RunShipState(
            runId,
            result.PullRequestNumber,
            result.PullRequestUrl,
            result.HeadBranch,
            ciStatus,
            CiLogsUrl: existing?.CiLogsUrl,
            DateTime.UtcNow,
            existing?.ManualStatusOverride);

        await _store.SaveAsync(state, ct).ConfigureAwait(false);
        await _fleet.Value.UpsertFromRunAsync(runId, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Recorded ship state for run {RunId}: pr={PrUrl} ci={CiStatus}",
            runId,
            result.PullRequestUrl,
            ciStatus);
    }

    public async Task ApplyCiWebhookAsync(GitHubCiWebhookPayload payload, CancellationToken ct = default)
    {
        var runId = payload.RunId ?? TryParseRunIdFromBranch(payload.HeadBranch);
        if (runId is null)
        {
            _logger.LogDebug("CI webhook ignored: no runId from branch {Branch}", payload.HeadBranch);
            return;
        }

        var ciStatus = MapCiConclusion(payload.Conclusion, payload.Action);
        var existing = await _store.GetAsync(runId.Value, ct).ConfigureAwait(false);
        var state = (existing ?? new RunShipState(
            runId.Value,
            null,
            null,
            payload.HeadBranch,
            FleetCiStatus.None,
            null,
            DateTime.UtcNow)) with
        {
            HeadBranch = payload.HeadBranch ?? existing?.HeadBranch,
            CiStatus = ciStatus,
            CiLogsUrl = payload.HtmlUrl ?? existing?.CiLogsUrl,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _store.SaveAsync(state, ct).ConfigureAwait(false);
        await _fleet.Value.UpsertFromRunAsync(runId.Value, ct).ConfigureAwait(false);

        if (ciStatus == FleetCiStatus.Failure
            && existing?.CiStatus != FleetCiStatus.Failure
            && _ciRepairOptions.AutoSpawnRepairOnCiFail)
        {
            _ciRepair?.DispatchCiFailureRepair(runId.Value, state.CiLogsUrl);
            _logger.LogInformation(
                "Dispatched CI failure repair for run {RunId} logs={LogsUrl}",
                runId,
                state.CiLogsUrl);
        }

        _logger.LogInformation(
            "Applied CI webhook for run {RunId}: conclusion={Conclusion} status={CiStatus}",
            runId,
            payload.Conclusion,
            ciStatus);
    }

    public AgentFleetStatus? ResolveStatus(AppGenerationOrchestrator? run, RunShipState? ship)
    {
        if (ship?.ManualStatusOverride is { } manual)
            return manual;

        if (ship is null)
            return null;

        if (ship.CiStatus == FleetCiStatus.Success && run?.Status == GenerationStatus.Completed)
            return AgentFleetStatus.Completed;

        if (ship.CiStatus == FleetCiStatus.Failure)
            return AgentFleetStatus.Failed;

        if (!string.IsNullOrWhiteSpace(ship.PullRequestUrl))
        {
            if (ship.CiStatus is FleetCiStatus.Pending or FleetCiStatus.InProgress)
                return AgentFleetStatus.WaitingForCi;
            return AgentFleetStatus.PrReady;
        }

        return null;
    }

    public static Guid? TryParseRunIdFromBranch(string? headBranch)
    {
        if (string.IsNullOrWhiteSpace(headBranch))
            return null;

        const string prefix = "libr4/autogen-";
        var normalized = headBranch.Trim().ToLowerInvariant();
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var token = normalized[prefix.Length..];
        return Guid.TryParse(token, out var runId) ? runId : null;
    }

    private static string MapCiConclusion(string? conclusion, string? action)
    {
        if (string.Equals(action, "requested", StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, "queued", StringComparison.OrdinalIgnoreCase))
            return FleetCiStatus.Pending;

        if (string.Equals(action, "in_progress", StringComparison.OrdinalIgnoreCase))
            return FleetCiStatus.InProgress;

        return conclusion?.ToLowerInvariant() switch
        {
            "success" => FleetCiStatus.Success,
            "failure" or "cancelled" or "timed_out" or "action_required" => FleetCiStatus.Failure,
            null or "" when string.Equals(action, "completed", StringComparison.OrdinalIgnoreCase) => FleetCiStatus.Failure,
            _ => FleetCiStatus.InProgress
        };
    }
}
