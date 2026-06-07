using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;
using DomainTechStack = Libr4.IDE.Domain.AutonomousAppGeneration.TechStack;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PhaseArtifactPathNormalizerTests
{
    private static GenerationPlan JavaReactPlan() => new(
        "MobileBankPro",
        "Banking app",
        new DomainTechStack(
            new[] { "Java", "TypeScript" },
            new[] { "Spring Boot", "React" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            "monorepo"),
        Array.Empty<GenerationPhase>(),
        Array.Empty<string>(),
        "eclipse-temurin:21-jdk",
        new[] { "cd backend && mvn -q package" },
        new[] { "cd frontend && npm test" },
        20);

    [Fact]
    public void NormalizeForPhase_Backend_JavaWithoutPrefix_GetsBackendPrefix()
    {
        var input = new List<DomainGeneratedFile>
        {
            new("src/main/java/com/app/App.java", "java", "package com.app;")
        };

        var result = PhaseArtifactPathNormalizer.NormalizeForPhase(AgentPhase.Backend, input, JavaReactPlan());

        result.Should().ContainSingle();
        result[0].RelativePath.Should().StartWith("backend/");
    }

    [Fact]
    public void NormalizeForPhase_Frontend_JavaFile_RelocatesToBackend()
    {
        var input = new List<DomainGeneratedFile>
        {
            new("frontend/src/Foo.java", "java", "class Foo {}")
        };

        var result = PhaseArtifactPathNormalizer.NormalizeForPhase(AgentPhase.Frontend, input, JavaReactPlan());

        result.Should().ContainSingle();
        result[0].RelativePath.Should().StartWith("backend/");
    }
}
