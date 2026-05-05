using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Handoff;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.PlanAgent;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AIToolingSynthesisPriority3Tests
{
    private sealed class FakePlanner : IAppPlannerService
    {
        public string LastRequest { get; private set; } = string.Empty;
        public GenerationPlan Result { get; set; } = new(
            "Default",
            "Default",
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, new[] { "PostgreSQL" }, new[] { "Docker" }, "default"),
            new List<GenerationPhase>(),
            new List<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            15);

        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
        {
            LastRequest = userRequest;
            return Task.FromResult(Result);
        }
    }

    [Fact]
    public async Task PlanAgentService_ShouldDecorateRequestAndDelegateToPlanner()
    {
        var planner = new FakePlanner();
        var expected = new GenerationPlan(
            "TestApp",
            "desc",
            new TechStack(new[] { "Python" }, new[] { "FastAPI" }, new[] { "PostgreSQL" }, new[] { "Docker" }, "r"),
            new List<GenerationPhase>(),
            new List<string>(),
            "python:3.12-slim",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "python -m pytest -q" },
            15);
        planner.Result = expected;

        var service = new PlanAgentService(planner);
        var result = await service.BuildPlanAsync("build billing app");

        result.Should().Be(expected);
        planner.LastRequest.Should().Contain("build billing app");
        planner.LastRequest.Should().Contain("[plan-agent]");
    }

    [Theory]
    [InlineData(30, 16000, false)]
    [InlineData(10, 50000, false)]
    [InlineData(12, 12000, true)]
    public void LocalCloudHandoffService_ShouldRouteLongOrHeavyRunsToCloud(int duration, int tokens, bool heavyGraph)
    {
        var service = new LocalCloudHandoffService();
        var decision = service.Decide(duration, tokens, heavyGraph);

        decision.Target.Should().Be(HandoffTarget.Cloud);
    }

    [Fact]
    public void LocalCloudHandoffService_ShouldKeepShortRunsLocal()
    {
        var service = new LocalCloudHandoffService();
        var decision = service.Decide(8, 6000, false);

        decision.Target.Should().Be(HandoffTarget.Local);
    }
}
