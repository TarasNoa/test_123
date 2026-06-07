using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

[Collection("DelegationManager")]
public sealed class DelegationParentIntegrationTests
{
    [Fact]
    public void PromptFormatter_IncludesSummaryAndFilePointer()
    {
        var section = DelegationPromptFormatter.FormatResultsSection(new DelegationNotification
        {
            DelegationId = "cool-red-owl",
            Summary = "found 3 API routes",
            CompletedAtUtc = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc),
            OutputRelativePath = "delegations/cool-red-owl.md"
        });

        section.Should().Contain("## delegation_results");
        section.Should().Contain("cool-red-owl");
        section.Should().Contain("found 3 API routes");
        section.Should().Contain("delegations/cool-red-owl.md");
        section.Should().Contain("delegation_read");
    }

    [Fact]
    public void AgentSpecRegistry_ImplementerIncludesDelegationTools()
    {
        var registry = new AgentSpecRegistry(
            Options.Create(new AgentSpecOptions { SpecsDirectory = ResolveSpecsDirectory() }),
            NullLogger<AgentSpecRegistry>.Instance);

        registry.TryGet("implementer", out var spec).Should().BeTrue();
        spec!.Toolset.Should().Contain("delegate");
        spec.Toolset.Should().Contain("delegation_list");
        spec.Toolset.Should().Contain("delegation_read");
    }

    [Fact]
    public void AgentSpecRegistry_RepairIncludesDelegationTools()
    {
        var registry = new AgentSpecRegistry(
            Options.Create(new AgentSpecOptions { SpecsDirectory = ResolveSpecsDirectory() }),
            NullLogger<AgentSpecRegistry>.Instance);

        registry.TryGet("repair", out var spec).Should().BeTrue();
        spec!.Toolset.Should().Contain("delegate");
        spec.Toolset.Should().Contain("delegation_list");
        spec.Toolset.Should().Contain("delegation_read");
    }

    [Fact]
    public async Task Manager_EnqueuesNotificationWithOutputPointer()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-parent-" + Guid.NewGuid().ToString("N"));
        var manager = CreateDelegationManager(root);
        var runId = Guid.NewGuid();

        await manager.StartExploreAsync(runId, "explore auth", _ => Task.FromResult("auth uses jwt"));
        await WaitForStatusAsync(manager, runId, DelegationStatuses.Completed);

        var notification = await manager.TryDequeueNotificationAsync(runId);
        notification.Should().NotBeNull();
        notification!.Summary.Should().Contain("auth");
        notification.OutputRelativePath.Should().Be("delegations/" + notification.DelegationId + ".md");
    }

    [Fact]
    public async Task Manager_DrainsAllPendingNotificationsAtTurnBoundary()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-parent-" + Guid.NewGuid().ToString("N"));
        var manager = CreateDelegationManager(root);
        var runId = Guid.NewGuid();

        await manager.StartExploreAsync(runId, "task-a", _ => Task.FromResult("a"));
        await manager.StartExploreAsync(runId, "task-b", _ => Task.FromResult("b"));
        await WaitForStatusAsync(manager, runId, DelegationStatuses.Completed, expectedCount: 2);

        var sections = new List<string>();
        while (true)
        {
            var notification = await manager.TryDequeueNotificationAsync(runId);
            if (notification is null)
                break;
            sections.Add(DelegationPromptFormatter.FormatResultsSection(notification));
        }

        sections.Should().HaveCount(2);
        sections.Should().OnlyContain(s => s.Contains("## delegation_results"));
    }

    [Fact]
    public async Task DelegateTool_DeniesNestedDelegationInBackgroundScope()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-del-parent-" + Guid.NewGuid().ToString("N"));
        var manager = CreateDelegationManager(root);
        var explore = new StubExploreRunner();
        var tool = new DelegateTool(manager, explore);
        var context = BuildToolContext(Guid.NewGuid());

        using (DelegationBackgroundContext.EnterChildScope())
        {
            var result = await tool.ExecuteAsync(
                JsonDocument.Parse("{\"task\":\"nested\"}").RootElement,
                context,
                CancellationToken.None);
            result.Success.Should().BeFalse();
            result.Output.Should().Contain("nested background delegation denied");
        }

        explore.Calls.Should().Be(0);
    }

    private static FileDelegationManager CreateDelegationManager(string runsRoot)
    {
        Environment.SetEnvironmentVariable("DELEGATE_BACKGROUND_CHILD", null);
        var workerHost = new ManagedDelegationWorkerHost(
            Options.Create(new DelegationRuntimeOptions()),
            NullLogger<ManagedDelegationWorkerHost>.Instance);

        return new FileDelegationManager(
            Options.Create(new AgentRuntimeOptions { RunsRoot = runsRoot }),
            Options.Create(new DelegationRuntimeOptions()),
            workerHost,
            NullLogger<FileDelegationManager>.Instance);
    }

    private static async Task WaitForStatusAsync(
        FileDelegationManager manager,
        Guid runId,
        string status,
        int expectedCount = 1,
        int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var list = await manager.ListAsync(runId);
            if (list.Count >= expectedCount && list.All(r => r.Status == status))
                return;

            await Task.Delay(50);
        }

        var final = await manager.ListAsync(runId);
        final.Should().HaveCountGreaterOrEqualTo(expectedCount);
        final.Should().OnlyContain(r => r.Status == status);
    }

    private static ToolContext BuildToolContext(Guid runId) =>
        new()
        {
            Workspace = new ShadowWorkspaceContext(runId, Path.GetTempPath(), string.Empty, NullRuntime.Instance),
            Accessor = NullAccessor.Instance,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Session = new AgentSessionState { RunId = runId },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };

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

    private sealed class StubExploreRunner : IDelegationExploreRunner
    {
        public int Calls { get; private set; }

        public Task<string> RunExploreAsync(string task, ToolContext context, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult("stub");
        }
    }

    private sealed class NullAccessor : IShadowWorkspaceAccessor
    {
        public static readonly NullAccessor Instance = new();

        public bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context)
        {
            context = default!;
            return false;
        }

        public Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern) =>
            Array.Empty<string>();
    }

    private sealed class NullRuntime : IRuntimeSession
    {
        public static readonly NullRuntime Instance = new();

        public string ProviderName => "null";
        public string SessionId => "null";
        public string HostMountPath => Path.GetTempPath();
        public string GuestMountPath => "/workspace";
        public string Image => "null";

        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
