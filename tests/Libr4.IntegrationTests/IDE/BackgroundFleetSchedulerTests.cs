using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

[Collection("DelegationManager")]
public sealed class BackgroundFleetSchedulerTests
{
    [Fact]
    public async Task SchedulesUserInitiatedBeforeScheduled()
    {
        var scheduler = CreateScheduler(maxGlobal: 2);
        var order = new List<string>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "scheduled-one", "s", DelegationFleetPriority.Scheduled),
            async ct =>
            {
                order.Add("scheduled-blocker");
                await gate.Task.WaitAsync(ct);
            });

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "user-one", "u", DelegationFleetPriority.UserInitiated),
            async ct =>
            {
                order.Add("user");
                await Task.Delay(10, ct);
            });

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "scheduled-two", "s2", DelegationFleetPriority.Scheduled),
            async ct =>
            {
                order.Add("scheduled-two");
                await Task.Delay(10, ct);
            });

        await WaitUntilAsync(() => order.Count >= 2);
        order.Should().Contain("user");
        order[0].Should().Be("scheduled-blocker");
        order[1].Should().Be("user");

        gate.TrySetResult();
    }

    [Fact]
    public async Task EnforcesPerTenantFairness()
    {
        var scheduler = CreateScheduler(maxGlobal: 4, maxPerTenant: 1);
        var tenantAStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tenantBStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runningTenants = new List<string>();

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "a1", "a", DelegationFleetPriority.UserInitiated, "tenant-a"),
            async ct =>
            {
                lock (runningTenants) runningTenants.Add("tenant-a");
                await tenantAStarted.Task.WaitAsync(ct);
            });

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "a2", "a2", DelegationFleetPriority.UserInitiated, "tenant-a"),
            _ => Task.CompletedTask);

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "b1", "b", DelegationFleetPriority.UserInitiated, "tenant-b"),
            async ct =>
            {
                lock (runningTenants) runningTenants.Add("tenant-b");
                await tenantBStarted.Task.WaitAsync(ct);
            });

        await WaitUntilAsync(() =>
        {
            lock (runningTenants)
                return runningTenants.Count >= 2;
        });

        lock (runningTenants)
            runningTenants.Should().BeEquivalentTo(["tenant-a", "tenant-b"]);

        tenantAStarted.TrySetResult();
        tenantBStarted.TrySetResult();
    }

    [Fact]
    public async Task PreemptsLowPriorityOnBudgetPressure()
    {
        var scheduler = CreateScheduler(maxGlobal: 2);
        var lowStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var preempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await scheduler.ScheduleAsync(
            new BackgroundDelegationRequest(Guid.NewGuid(), "retry-one", "r", DelegationFleetPriority.Retry),
            async ct =>
            {
                lowStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    preempted.TrySetResult();
                    throw;
                }
            });

        await lowStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        scheduler.RaiseImplementerBudgetPressure(Guid.NewGuid(), "tenant-x");
        await preempted.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static BackgroundFleetScheduler CreateScheduler(int maxGlobal = 6, int maxPerTenant = 2) =>
        new(
            Options.Create(new DelegationRuntimeOptions
            {
                MaxGlobalConcurrentDelegations = maxGlobal,
                MaxConcurrentDelegationsPerTenant = maxPerTenant,
                EnableFleetScheduler = true
            }),
            NullLogger<BackgroundFleetScheduler>.Instance);

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return;

            await Task.Delay(25);
        }

        predicate().Should().BeTrue("condition timed out");
    }
}
