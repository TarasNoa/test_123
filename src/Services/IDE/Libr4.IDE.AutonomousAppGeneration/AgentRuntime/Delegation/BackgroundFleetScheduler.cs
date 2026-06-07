using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public sealed class BackgroundFleetScheduler : IBackgroundFleetScheduler
{
    private readonly DelegationRuntimeOptions _options;
    private readonly ILogger<BackgroundFleetScheduler> _logger;
    private readonly object _sync = new();
    private readonly List<PendingJob> _pending = new();
    private readonly Dictionary<string, RunningJob> _running = new(StringComparer.OrdinalIgnoreCase);
    private int _globalRunning;

    public BackgroundFleetScheduler(
        IOptions<DelegationRuntimeOptions> options,
        ILogger<BackgroundFleetScheduler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task ScheduleAsync(
        BackgroundDelegationRequest request,
        Func<CancellationToken, Task> executeAsync,
        CancellationToken ct = default)
    {
        lock (_sync)
        {
            _pending.Add(new PendingJob(request, executeAsync, DateTime.UtcNow));
        }

        _ = ProcessQueueAsync(CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task<BackgroundFleetSummary> GetSummaryAsync(BackgroundFleetListQuery query, CancellationToken ct = default)
    {
        lock (_sync)
        {
            var items = new List<BackgroundDelegationSnapshot>();

            foreach (var pending in _pending)
            {
                if (!Matches(pending.Request, query))
                    continue;

                items.Add(new BackgroundDelegationSnapshot(
                    pending.Request.RunId,
                    pending.Request.DelegationId,
                    pending.Request.Task,
                    "queued",
                    pending.Request.Priority,
                    pending.Request.TenantUserId,
                    pending.EnqueuedAtUtc,
                    null));
            }

            foreach (var running in _running.Values)
            {
                if (!Matches(running.Request, query))
                    continue;

                items.Add(new BackgroundDelegationSnapshot(
                    running.Request.RunId,
                    running.Request.DelegationId,
                    running.Request.Task,
                    "running",
                    running.Request.Priority,
                    running.Request.TenantUserId,
                    running.EnqueuedAtUtc,
                    running.StartedAtUtc));
            }

            items.Sort((a, b) =>
            {
                var priority = a.Priority.CompareTo(b.Priority);
                return priority != 0 ? priority : a.EnqueuedAtUtc.CompareTo(b.EnqueuedAtUtc);
            });

            if (query.ActiveOnly)
                items = items.Where(i => i.QueueStatus is "queued" or "running").ToList();

            return Task.FromResult(new BackgroundFleetSummary(
                _running.Count,
                _pending.Count,
                items));
        }
    }

    public void RaiseImplementerBudgetPressure(Guid runId, string? tenantUserId = null)
    {
        List<RunningJob> victims;
        lock (_sync)
        {
            victims = _running.Values
                .Where(r => r.Request.Priority >= DelegationFleetPriority.Scheduled)
                .OrderByDescending(r => r.Request.Priority)
                .ThenByDescending(r => r.StartedAtUtc)
                .ToList();
        }

        foreach (var victim in victims)
        {
            _logger.LogInformation(
                "Preempting background delegation {DelegationId} for implementer budget (run={RunId}, tenant={Tenant})",
                victim.Request.DelegationId,
                runId,
                tenantUserId ?? victim.Request.TenantUserId ?? "unknown");

            victim.Cts.Cancel();
        }
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        PendingJob? next = null;
        CancellationTokenSource? runCts = null;

        lock (_sync)
        {
            if (_globalRunning >= Math.Max(1, _options.MaxGlobalConcurrentDelegations))
                return;

            next = PickNextJobLocked();
            if (next is null)
                return;

            _pending.Remove(next);
            runCts = new CancellationTokenSource();
            _running[next.Request.DelegationId] = new RunningJob(
                next.Request,
                next.EnqueuedAtUtc,
                DateTime.UtcNow,
                runCts);
            _globalRunning++;
        }

        try
        {
            await next.ExecuteAsync(runCts!.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (runCts!.IsCancellationRequested)
        {
            _logger.LogDebug("Background delegation {DelegationId} cancelled", next.Request.DelegationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background delegation {DelegationId} failed in scheduler", next.Request.DelegationId);
        }
        finally
        {
            lock (_sync)
            {
                _running.Remove(next!.Request.DelegationId);
                _globalRunning = Math.Max(0, _globalRunning - 1);
            }

            runCts!.Dispose();
            _ = ProcessQueueAsync(CancellationToken.None);
        }
    }

    private PendingJob? PickNextJobLocked()
    {
        if (_pending.Count == 0)
            return null;

        var tenantRunning = _running.Values
            .GroupBy(r => NormalizeTenant(r.Request.TenantUserId))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return _pending
            .OrderBy(j => j.Request.Priority)
            .ThenBy(j => tenantRunning.GetValueOrDefault(NormalizeTenant(j.Request.TenantUserId)))
            .ThenBy(j => j.EnqueuedAtUtc)
            .FirstOrDefault(j =>
                tenantRunning.GetValueOrDefault(NormalizeTenant(j.Request.TenantUserId))
                < Math.Max(1, _options.MaxConcurrentDelegationsPerTenant));
    }

    private static bool Matches(BackgroundDelegationRequest request, BackgroundFleetListQuery query)
    {
        if (query.RunId is Guid runId && request.RunId != runId)
            return false;

        if (!string.IsNullOrWhiteSpace(query.TenantUserId)
            && !string.Equals(request.TenantUserId, query.TenantUserId, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string NormalizeTenant(string? tenantUserId) =>
        string.IsNullOrWhiteSpace(tenantUserId) ? "__default__" : tenantUserId.Trim();

    private sealed record PendingJob(
        BackgroundDelegationRequest Request,
        Func<CancellationToken, Task> ExecuteAsync,
        DateTime EnqueuedAtUtc);

    private sealed record RunningJob(
        BackgroundDelegationRequest Request,
        DateTime EnqueuedAtUtc,
        DateTime StartedAtUtc,
        CancellationTokenSource Cts);
}
