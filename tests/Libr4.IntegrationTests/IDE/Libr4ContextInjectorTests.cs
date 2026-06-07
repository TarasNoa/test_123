using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class Libr4ContextInjectorTests
{
    private readonly Libr4ContextInjector _injector = new(Options.Create(new Libr4ContextOptions
    {
        EnableJitInjection = true,
        MaxCharsPerInjection = 2000
    }));

    [Fact]
    public void ResolveMergedContext_OrdersRootThenBackendOverride()
    {
        var working = new[]
        {
            new GeneratedFile("LIBR4.md", "markdown", "# CalorieVision monorepo\nbackend + frontend"),
            new GeneratedFile("backend/LIBR4.override.md", "markdown", "OpenAI vision logic in meals/services")
        };

        var merged = _injector.ResolveMergedContext(
            "backend/meals/views.py",
            workspaceHostPath: string.Empty,
            working);

        merged.IndexOf("LIBR4.md").Should().BeLessThan(merged.IndexOf("backend/LIBR4.override.md"));
        merged.Should().Contain("CalorieVision monorepo");
        merged.Should().Contain("OpenAI vision logic");
    }

    [Fact]
    public void TryInjectForPath_TruncatesToBudget()
    {
        var huge = new string('x', 5000);
        var working = new[] { new GeneratedFile("LIBR4.md", "markdown", huge) };

        _injector.TryInjectForPath("backend/meals/views.py", string.Empty, working, out var formatted)
            .Should().BeTrue();
        formatted.Length.Should().BeLessOrEqualTo(2004);
        formatted.Should().EndWith("…");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesJitVariablePlaceholder()
    {
        var resolver = new BuiltinPromptVarResolver();
        var context = new BuiltinPromptVarContext
        {
            JitLibr4Context = "## backend/LIBR4.override.md\nuse meals package"
        };

        var prompt = AgentPromptBuilder.BuildSystemPrompt(
            isGeneration: false,
            BuiltinPromptStage.Repairing,
            resolver,
            context);

        prompt.Should().Contain("use meals package");
        prompt.Should().Contain("Directory context (jit)");
    }

    [Fact]
    public void Libr4MdManifest_SeedsCalorieVisionOverrides()
    {
        var plan = new GenerationPlan(
            applicationName: "CalorieVision",
            applicationDescription: "Calorie tracker",
            techStack: new TechStack(
                new[] { "Python", "TypeScript" },
                new[] { "Django", "SolidJS" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "django+solidjs"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>());

        var seed = Libr4MdManifest.SeedContentForPlan(plan);
        seed.Should().Contain(f => f.RelativePath == "LIBR4.md");
        seed.Should().Contain(f => f.RelativePath == "backend/LIBR4.override.md");
        seed.Should().Contain(f => f.RelativePath == "frontend/LIBR4.override.md");
    }
}
