using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class IncrementalGenerationSeedPolicyTests
{
    private const string CalorieRequest =
        "Строго Django backend + SolidJS frontend (backend/ + frontend/). " +
        "Не использовать React, Vue, NestJS. TypeScript + Python.";

    [Fact]
    public void ResolveSeedFiles_ReturnsEmpty_WhenSeedModeNone()
    {
        var plan = new GenerationPlan(
            "CalorieVision",
            "Django + SolidJS app",
            new Libr4.IDE.Domain.AutonomousAppGeneration.TechStack(
                new[] { "Python", "TypeScript" },
                new[] { "Django", "SolidJS" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12-slim",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            IncrementalSeedMode = IncrementalSeedMode.None
        };

        var seeds = IncrementalGenerationSeedPolicy.ResolveSeedFiles(plan, Array.Empty<DomainGeneratedFile>(), options);

        seeds.Should().BeEmpty();
        IncrementalGenerationSeedPolicy.ShouldUseStackSafetyNet(plan, options).Should().BeFalse();
    }

    [Fact]
    public void ResolveSeedFiles_ReturnsEmpty_ForStrictStackContract_EvenWhenConfigMinimalSpine()
    {
        var plan = StrictStackContractEnforcer.Enforce(
            new GenerationPlan(
                "CalorieVision",
                "Calorie app",
                new Libr4.IDE.Domain.AutonomousAppGeneration.TechStack(
                    new[] { "Python", "TypeScript" },
                    new[] { "Django", "SolidJS" },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "test"),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "python:3.12-slim",
                Array.Empty<string>(),
                Array.Empty<string>(),
                5),
            CalorieRequest);

        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            IncrementalSeedMode = IncrementalSeedMode.MinimalSpine
        };

        IncrementalGenerationSeedPolicy.ResolveEffectiveSeedMode(plan, options)
            .Should().Be(IncrementalSeedMode.None);
        IncrementalGenerationSeedPolicy.ResolveSeedFiles(plan, Array.Empty<DomainGeneratedFile>(), options)
            .Should().BeEmpty();
    }

    [Fact]
    public void ResolveSeedFiles_StillSeeds_JavaReact_WhenFullSafetyNetConfigured()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "MobileBankApp",
                "Java Spring Boot + React banking",
                StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "eclipse-temurin:21-jdk",
                Array.Empty<string>(),
                Array.Empty<string>(),
                6),
            "java react banking");

        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            UseExpandedJavaReactManifest = true,
            IncrementalSeedMode = IncrementalSeedMode.FullSafetyNet
        };

        var seeds = IncrementalGenerationSeedPolicy.ResolveSeedFiles(plan, Array.Empty<DomainGeneratedFile>(), options);

        seeds.Should().NotBeEmpty();
        seeds.Should().Contain(f => f.RelativePath.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase));
        IncrementalGenerationSeedPolicy.ShouldUseStackSafetyNet(plan, options).Should().BeTrue();
    }
}
