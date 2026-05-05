using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Manager;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.MultiRepo;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Unix;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AIToolingSynthesisPriority2Tests
{
    [Fact]
    public void MultiRepoWorkspaceRegistry_ShouldStoreAndResolveWorkspaces()
    {
        var registry = new MultiRepoWorkspaceRegistry();
        registry.Register(new RepoWorkspace("primary", "d:/repo-main", true));
        registry.Register(new RepoWorkspace("infra", "d:/repo-infra", false));

        registry.Get("primary").Should().NotBeNull();
        registry.Get("primary")!.IsPrimary.Should().BeTrue();
        registry.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void ManagerSurfaceService_ShouldTrackAsyncTaskLifecycle()
    {
        var manager = new ManagerSurfaceService();
        var task = manager.Enqueue("design-agent", "prepare dashboard artifact");

        task.Status.Should().Be(ManagedAgentStatus.Pending);
        var running = manager.UpdateStatus(task.Id, ManagedAgentStatus.Running);
        var done = manager.UpdateStatus(task.Id, ManagedAgentStatus.Completed);

        running.Status.Should().Be(ManagedAgentStatus.Running);
        done.Status.Should().Be(ManagedAgentStatus.Completed);
        manager.List().Should().ContainSingle(x => x.Id == task.Id);
    }

    [Fact]
    public void UnixComposableTaskRunner_ShouldComposePipelineStepsDeterministically()
    {
        var runner = new UnixComposableTaskRunner();
        var output = runner.Run(
            "  hello world  ",
            new[]
            {
                new UnixTaskStep("trim", s => s.Trim()),
                new UnixTaskStep("upper", s => s.ToUpperInvariant()),
                new UnixTaskStep("prefix", s => $"OUT:{s}")
            });

        output.Should().Be("OUT:HELLO WORLD");
    }
}
