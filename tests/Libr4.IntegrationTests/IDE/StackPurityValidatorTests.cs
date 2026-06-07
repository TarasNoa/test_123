using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StackPurityValidatorTests
{
    [Fact]
    public void ValidateAndPrune_RemovesCsArtifactsFromNodeStack()
    {
        var plan = new GenerationPlan(
            "ExpressApi",
            "express api",
            new TechStack(
                new[] { "typescript" },
                new[] { "express" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:20",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var files = new List<GeneratedFile>
        {
            new("backend/package.json", "json", "{}"),
            new("backend/Program.cs", "csharp", "namespace X; class Program {}"),
            new("backend/server.ts", "typescript", "import express from 'express';")
        };

        var result = StackPurityValidator.ValidateAndPrune(files, plan, autoPrune: true);
        result.FilesRemoved.Should().Be(1);
        result.Findings.Should().Contain(f => f.FilePath == "backend/Program.cs");
        files.Should().NotContain(f => f.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }
}
