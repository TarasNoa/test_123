using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PlanCommandValidationStageTests
{
    private static PlanCommandValidationStage CreateSut(bool benchmarkMode = true) =>
        new(
            new DefaultPlanCommandValidator(),
            Options.Create(new AutonomousBenchmarkModeOptions
            {
                EnableBenchmarkMode = benchmarkMode,
                UseSafeDefaultsOnPlanValidationFailure = true
            }),
            NullLogger<PlanCommandValidationStage>.Instance);

    [Fact]
    public async Task Execute_ValidPlan_ReturnsContinue_WithoutMutation()
    {
        var plan = MakePlan(buildCommands: new[] { "dotnet build" }, testCommands: new[] { "dotnet test" });
        var ctx = new GenerationContext
        {
            Orchestrator = AppGenerationOrchestrator.Create("test", "fp"),
            UserRequest = "test",
            Plan = plan
        };

        var outcome = await CreateSut().ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.Plan.Should().BeSameAs(plan);
        ctx.Orchestrator.PipelineStageReached.Should().Be(AutonomousPipelineStages.Planning);
    }

    [Fact]
    public async Task Execute_MalformedCommands_SubstitutesSafeDefaults_AndContinues()
    {
        var plan = MakePlan(
            buildCommands: new[] { "dotnet 'restore" },
            testCommands: new[] { "dotnet test" });
        var ctx = new GenerationContext
        {
            Orchestrator = AppGenerationOrchestrator.Create("test", "fp"),
            UserRequest = "test",
            Plan = plan
        };

        var outcome = await CreateSut().ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.Plan.Should().NotBeSameAs(plan);
        ctx.Plan!.BuildCommands.Should().Contain("dotnet restore");
        ctx.Orchestrator.QualityGates.Should().Contain(g => g.Stage == "plan_command_validation");
    }

    [Fact]
    public async Task Execute_NullPlan_ReturnsContinue()
    {
        var ctx = new GenerationContext
        {
            Orchestrator = AppGenerationOrchestrator.Create("test", "fp"),
            UserRequest = "test",
            Plan = null
        };

        var outcome = await CreateSut().ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
    }

    private static GenerationPlan MakePlan(IReadOnlyList<string> buildCommands, IReadOnlyList<string> testCommands)
    {
        var stack = new TechStack(
            new[] { "C#" },
            new[] { "ASP.NET Core" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            "test");
        return new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: stack,
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: buildCommands,
            testCommands: testCommands,
            maxIterations: 10);
    }
}
