using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class TaskGraphHydrationServiceTests
{
    [Fact]
    public void EnsureHydrated_RestoresFromPersistedJson_WhenInMemoryGraphEmpty()
    {
        var orchestrator = AppGenerationOrchestrator.Create("app", "fp-1");
        orchestrator.ReplaceTaskGraph(new[]
        {
            new AgentTaskGraphEntry("t_plan", "Plan", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null)
        });

        var json = orchestrator.TaskGraphPersistedJson;
        json.Should().NotBeNullOrWhiteSpace();

        var fresh = AppGenerationOrchestrator.Create("app", "fp-2");
        typeof(AppGenerationOrchestrator)
            .GetProperty(nameof(AppGenerationOrchestrator.TaskGraphPersistedJson))!
            .SetValue(fresh, json);

        var hydration = new TaskGraphHydrationService();
        hydration.EnsureHydrated(fresh);

        fresh.TaskGraph.Should().ContainSingle(t => t.TaskId == "t_plan");
    }

    [Fact]
    public void Resolve_SynthesizesFromPlan_WhenNoSnapshot()
    {
        var orchestrator = AppGenerationOrchestrator.Create("app", "fp-3");
        orchestrator.AttachPlan(new GenerationPlan(
            "App",
            "desc",
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "t"),
            new[]
            {
                new GenerationPhase(1, "bootstrap", "clone", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "tests", "verify", Array.Empty<AgentAssignment>()),
            },
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            Array.Empty<string>(),
            3));

        var hydration = new TaskGraphHydrationService();
        var graph = hydration.Resolve(orchestrator);

        graph.Should().HaveCount(2);
        graph[1].BlockedByTaskIds.Should().Contain("t_phase_1");
    }
}
