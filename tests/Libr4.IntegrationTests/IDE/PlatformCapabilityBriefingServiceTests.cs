using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PlatformCapabilityBriefingServiceTests
{
    [Fact]
    public void ScopedBriefing_IncludesWhenUseAndSkipsFullCatalog()
    {
        var service = CreateService(PlatformCapabilityBriefingMode.Scoped);
        var plan = FastApiPlan();
        var briefing = service.BuildBriefing(new PlatformCapabilityBriefingRequest(
            PlatformCapabilityBriefingStage.Repair,
            plan));

        briefing.Should().Contain("USE:");
        briefing.Should().Contain("SKIP:");
        briefing.Should().Contain("python-fastapi");
        briefing.Should().Contain("tool_search");
        briefing.Should().NotContain("## Agent tools");
        briefing.Length.Should().BeLessThan(4500);
    }

    [Fact]
    public void ScopedBriefing_FiltersByStage()
    {
        var service = CreateService(PlatformCapabilityBriefingMode.Scoped);
        var plan = FastApiPlan();

        var repair = service.BuildBriefing(new PlatformCapabilityBriefingRequest(
            PlatformCapabilityBriefingStage.Repair, plan));
        var planning = service.BuildBriefing(new PlatformCapabilityBriefingRequest(
            PlatformCapabilityBriefingStage.Planning, plan));

        repair.Should().Contain("bash");
        planning.Should().NotContain("delegate");
    }

    [Fact]
    public void FullMode_StillDumpsToolCatalog()
    {
        var service = CreateService(PlatformCapabilityBriefingMode.Full);
        var briefing = service.BuildBriefing();

        briefing.Should().Contain("## Agent tools");
    }

    private static PlatformCapabilityBriefingService CreateService(PlatformCapabilityBriefingMode mode)
    {
        var tools = new AgentToolRegistry(Array.Empty<IAgentTool>());
        var skills = new FileSkillManifestRegistry(Options.Create(new SkillActivationOptions()));
        var specs = new AgentSpecRegistry(
            Options.Create(new AgentSpecOptions { SubAgents = [] }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AgentSpecRegistry>.Instance);
        return new PlatformCapabilityBriefingService(
            tools,
            skills,
            specs,
            Options.Create(new AutonomousPlatformUtilizationOptions
            {
                CapabilityBriefingMode = mode,
                MaxBriefingChars = 4500
            }));
    }

    private static GenerationPlan FastApiPlan() =>
        new(
            applicationName: "crm",
            applicationDescription: "FastAPI CRM",
            techStack: new TechStack(["Python"], ["FastAPI"], [], [], "fastapi"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12",
            buildCommands: ["pip install -r requirements.txt"],
            testCommands: ["pytest"],
            maxIterations: 5);
}
