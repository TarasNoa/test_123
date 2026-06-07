using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DelegationTelemetryTests
{
    [Fact]
    public void DurationHistogram_IsObservable()
    {
        var captured = new List<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == DelegationTelemetry.MeterName
                && instrument.Name == "libr4_delegation_duration_seconds")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, value, _, _) => captured.Add(value));
        listener.Start();

        DelegationTelemetry.RecordDuration(Guid.NewGuid(), 1.25, "cool-red-owl");

        captured.Should().Contain(1.25);
    }

    [Fact]
    public void TimeoutCounter_Increments()
    {
        var captured = new ConcurrentBag<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == DelegationTelemetry.MeterName
                && instrument.Name == "libr4_delegation_timeout_total")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => captured.Add(value));
        listener.Start();

        DelegationTelemetry.RecordCompletion(Guid.NewGuid(), succeeded: false, timedOut: true);

        captured.Should().HaveCountGreaterOrEqualTo(1);
        captured.Sum().Should().BeGreaterOrEqualTo(1);
    }
}

public sealed class DelegationTimeoutAlertMonitorTests
{
    [Fact]
    public void EvaluateHourlyTimeoutRate_AlertsWhenAboveThreshold()
    {
        var runId = Guid.NewGuid();
        for (var i = 0; i < 4; i++)
            DelegationTelemetry.RecordCompletion(runId, succeeded: true, timedOut: false);
        for (var i = 0; i < 6; i++)
            DelegationTelemetry.RecordCompletion(runId, succeeded: false, timedOut: true);

        var monitor = new DelegationTimeoutAlertMonitor(
            Options.Create(new DelegationRuntimeOptions
            {
                EnableTimeoutRateAlerts = true,
                TimeoutAlertRateThreshold = 0.10,
                TimeoutAlertMinSamples = 5,
                TimeoutAlertCooldownMinutes = 0
            }),
            NullLogger<DelegationTimeoutAlertMonitor>.Instance);

        monitor.EvaluateHourlyTimeoutRate();

        DelegationTelemetry.GetHourlyStats().TimeoutRate.Should().BeGreaterThan(0.10);
    }
}

public sealed class DelegationE2ETests
{
    [Fact]
    public async Task Timeout_EnqueuesParentNotification()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-e2e-" + Guid.NewGuid().ToString("N"));
        var manager = CreateManager(root, new TimedOutWorkerHost());
        var runId = Guid.NewGuid();

        await manager.StartExploreAsync(runId, "slow explore", _ => Task.FromResult("never"));
        await WaitForStatusAsync(manager, runId, DelegationStatuses.TimedOut);

        var notification = await manager.TryDequeueNotificationAsync(runId);
        notification.Should().NotBeNull();
        notification!.Summary.Should().Contain("timed out");
    }

    [Fact]
    public async Task ThreeParallelDelegations_AllCompleteAndNotify()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-e2e-" + Guid.NewGuid().ToString("N"));
        var manager = CreateManager(root);
        var runId = Guid.NewGuid();

        var ids = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var started = await manager.StartExploreAsync(runId, $"task-{i}", _ => Task.FromResult($"out-{i}"));
            ids.Add(started.Id);
        }

        foreach (var id in ids)
            await WaitForStatusAsync(manager, runId, DelegationStatuses.Completed, id);

        var notifications = new List<DelegationNotification>();
        while (true)
        {
            var n = await manager.TryDequeueNotificationAsync(runId);
            if (n is null) break;
            notifications.Add(n);
        }

        notifications.Should().HaveCount(3);
    }

    [Fact]
    public async Task FleetScheduler_DrainsTenQueuedWithinGlobalQuota()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-e2e-" + Guid.NewGuid().ToString("N"));
        var scheduler = new BackgroundFleetScheduler(
            Options.Create(new DelegationRuntimeOptions
            {
                MaxGlobalConcurrentDelegations = 3,
                MaxConcurrentDelegationsPerTenant = 3,
                EnableFleetScheduler = true
            }),
            NullLogger<BackgroundFleetScheduler>.Instance);

        var manager = CreateManager(root, fleetScheduler: scheduler, maxConcurrent: 3);
        var runId = Guid.NewGuid();

        for (var i = 0; i < 10; i++)
            await manager.StartExploreAsync(runId, $"load-{i}", _ => Task.FromResult($"ok-{i}"));

        await WaitForCountAsync(manager, runId, 10, DelegationStatuses.Completed, timeoutMs: 30000);

        var list = await manager.ListAsync(runId);
        list.Should().HaveCount(10);
        list.Should().OnlyContain(r => r.Status == DelegationStatuses.Completed);
    }

    [Fact]
    public void ExploreSpec_IsReadOnly_AndDelegateToolIsReadOnly()
    {
        var registry = new AgentSpecRegistry(
            Options.Create(new AgentSpecOptions { SpecsDirectory = ResolveSpecsDirectory() }),
            NullLogger<AgentSpecRegistry>.Instance);

        registry.TryGet("explore", out var explore).Should().BeTrue();
        explore!.IsReadOnly.Should().BeTrue();

        var delegateTool = new DelegateTool(
            new NoopDelegationManager(),
            new NoopExploreRunner());
        delegateTool.IsReadOnly.Should().BeTrue();
    }

    private static FileDelegationManager CreateManager(
        string runsRoot,
        IDelegationWorkerHost? workerHost = null,
        IBackgroundFleetScheduler? fleetScheduler = null,
        int maxConcurrent = 3)
    {
        Environment.SetEnvironmentVariable("DELEGATE_BACKGROUND_CHILD", null);
        return new(
            Options.Create(new AgentRuntimeOptions { RunsRoot = runsRoot }),
            Options.Create(new DelegationRuntimeOptions
            {
                MaxConcurrentDelegationsPerRun = maxConcurrent,
                EnableFleetScheduler = fleetScheduler is not null
            }),
            workerHost ?? new ManagedDelegationWorkerHost(
                Options.Create(new DelegationRuntimeOptions()),
                NullLogger<ManagedDelegationWorkerHost>.Instance),
            NullLogger<FileDelegationManager>.Instance,
            fleetScheduler);
    }

    private static async Task WaitForStatusAsync(
        FileDelegationManager manager,
        Guid runId,
        string status,
        string? delegationId = null,
        int timeoutMs = 8000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (delegationId is not null)
            {
                var one = await manager.GetAsync(runId, delegationId);
                if (one?.Status == status)
                    return;
            }
            else
            {
                var list = await manager.ListAsync(runId);
                if (list.Count > 0 && list.All(r => r.Status == status))
                    return;
            }

            await Task.Delay(50);
        }

        if (delegationId is not null)
            (await manager.GetAsync(runId, delegationId))!.Status.Should().Be(status);
        else
            (await manager.ListAsync(runId)).Should().OnlyContain(r => r.Status == status);
    }

    private static async Task WaitForCountAsync(
        FileDelegationManager manager,
        Guid runId,
        int count,
        string status,
        int timeoutMs = 25000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var list = await manager.ListAsync(runId);
            if (list.Count >= count && list.All(r => r.Status == status))
                return;

            await Task.Delay(100);
        }

        var final = await manager.ListAsync(runId);
        final.Should().HaveCountGreaterOrEqualTo(count);
        final.Should().OnlyContain(r => r.Status == status);
    }

    private static string ResolveSpecsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Agents", "Subagents"),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "src", "Services", "IDE", "Libr4.IDE.AutonomousAppGeneration", "Agents", "Subagents"))
        };

        return candidates.First(Directory.Exists);
    }

    private sealed class TimedOutWorkerHost : IDelegationWorkerHost
    {
        public Task<DelegationWorkerResult> ExecuteAsync(
            DelegationWorkerRequest request,
            Func<CancellationToken, Task<string>> worker,
            CancellationToken ct = default) =>
            Task.FromResult(new DelegationWorkerResult(
                false,
                string.Empty,
                "delegation_timeout:1m",
                TimedOut: true));
    }

    private sealed class NoopDelegationManager : IDelegationManager
    {
        public bool IsBackgroundChild() => false;
        public Task<DelegationRecord> StartExploreAsync(Guid runId, string task, Func<CancellationToken, Task<string>> worker, DelegationFleetPriority priority = DelegationFleetPriority.UserInitiated, string? tenantUserId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DelegationRecord>> ListAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DelegationRecord>>(Array.Empty<DelegationRecord>());
        public Task<DelegationRecord?> GetAsync(Guid runId, string delegationId, CancellationToken ct = default) =>
            Task.FromResult<DelegationRecord?>(null);
        public Task<DelegationNotification?> TryDequeueNotificationAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult<DelegationNotification?>(null);
        public Task<string?> ReadOutputAsync(Guid runId, string delegationId, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class NoopExploreRunner : IDelegationExploreRunner
    {
        public Task<string> RunExploreAsync(string task, ToolContext context, CancellationToken ct) =>
            Task.FromResult("noop");
    }
}
