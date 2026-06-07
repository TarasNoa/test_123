using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

[Collection("DelegationManager")]
public sealed class DelegationManagerTests
{
    [Fact]
    public async Task StartsAndLists()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-" + Guid.NewGuid().ToString("N"));
        var manager = CreateDelegationManager(root);
        var runId = Guid.NewGuid();
        var started = await manager.StartExploreAsync(runId, "explore api", _ => Task.FromResult("ok"));
        started.Status.Should().Be(DelegationStatuses.Queued);
        var list = await manager.ListAsync(runId);
        list.Should().HaveCount(1);
        await WaitForDelegationStatusAsync(manager, runId, started.Id, DelegationStatuses.Completed);
        list = await manager.ListAsync(runId);
        list[0].Status.Should().Be(DelegationStatuses.Completed);
        list[0].OutputPreview.Should().Be("ok");
    }

    [Fact]
    public async Task RespectsConcurrencyLimit()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-" + Guid.NewGuid().ToString("N"));
        var gate = new SemaphoreSlim(0);
        var manager = CreateDelegationManager(root, maxConcurrent: 1);
        var runId = Guid.NewGuid();

        var first = await manager.StartExploreAsync(runId, "slow", async ct =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            return "first";
        });
        await WaitForDelegationStatusAsync(manager, runId, first.Id, DelegationStatuses.Running, timeoutMs: 2000);
        var second = await manager.StartExploreAsync(runId, "fast", _ => Task.FromResult("second"));

        var firstRecord = await manager.GetAsync(runId, first.Id);
        var secondRecord = await manager.GetAsync(runId, second.Id);
        firstRecord.Should().NotBeNull();
        firstRecord!.Status.Should().Be(DelegationStatuses.Running);
        secondRecord.Should().NotBeNull();
        secondRecord!.Status.Should().Be(DelegationStatuses.Queued);

        gate.Release();
        await WaitForDelegationStatusAsync(manager, runId, first.Id, DelegationStatuses.Completed);
        await WaitForDelegationStatusAsync(manager, runId, second.Id, DelegationStatuses.Completed);
    }

    [Fact]
    public async Task RetriesOnceThenFails()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-" + Guid.NewGuid().ToString("N"));
        var attempts = 0;
        var manager = CreateDelegationManager(root, maxRestartAttempts: 1);
        var runId = Guid.NewGuid();

        await manager.StartExploreAsync(runId, "flaky", _ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        });

        await WaitForDelegationStatusAsync(manager, runId, (await manager.ListAsync(runId))[0].Id, DelegationStatuses.Failed);
        attempts.Should().Be(2);
    }

    private static FileDelegationManager CreateDelegationManager(
        string runsRoot,
        int maxConcurrent = 3,
        int maxRestartAttempts = 0)
    {
        Environment.SetEnvironmentVariable("DELEGATE_BACKGROUND_CHILD", null);

        var workerHost = new ManagedDelegationWorkerHost(
            Options.Create(new DelegationRuntimeOptions { MaxRestartAttempts = maxRestartAttempts }),
            NullLogger<ManagedDelegationWorkerHost>.Instance);

        return new FileDelegationManager(
            Options.Create(new AgentRuntimeOptions { RunsRoot = runsRoot }),
            Options.Create(new DelegationRuntimeOptions
            {
                MaxConcurrentDelegationsPerRun = maxConcurrent,
                MaxRestartAttempts = maxRestartAttempts
            }),
            workerHost,
            NullLogger<FileDelegationManager>.Instance);
    }

    private static async Task WaitForDelegationStatusAsync(
        FileDelegationManager manager,
        Guid runId,
        string delegationId,
        string expectedStatus,
        int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var record = await manager.GetAsync(runId, delegationId);
            if (record?.Status == expectedStatus)
                return;

            await Task.Delay(50);
        }

        var finalRecord = await manager.GetAsync(runId, delegationId);
        finalRecord.Should().NotBeNull();
        finalRecord!.Status.Should().Be(expectedStatus);
    }
}

[CollectionDefinition("DelegationManager", DisableParallelization = true)]
public sealed class DelegationManagerCollection;
