using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StackArtifactRecoveryRouterTests
{
    [Fact]
    public void Normalize_DotNet_RemovesDuplicateProgram()
    {
        var plan = new GenerationPlan(
            "Api",
            "ASP.NET Core API",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "dotnet 8"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var files = new List<GeneratedFile>
        {
            new("src/Api/Program.cs", "csharp", "var builder = WebApplication.CreateBuilder(args);"),
            new("backup/Program.cs", "csharp", "var builder = WebApplication.CreateBuilder(args);"),
        };

        var report = StackArtifactRecoveryRouter.Normalize(files, plan);
        report.Stack.Should().Be(StackKind.DotNet);
        files.Should().ContainSingle(f => f.Content!.Contains("WebApplication.CreateBuilder"));
    }

    [Fact]
    public void Normalize_Python_RemovesDuplicateRequirements()
    {
        var plan = new GenerationPlan(
            "PyApi",
            "FastAPI service",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "python:3.12"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi\nuvicorn\n"),
            new("app/requirements.txt", "text", "fastapi\n"),
            new("main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("legacy.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
        };

        StackArtifactRecoveryRouter.Normalize(files, plan);
        files.Should().ContainSingle(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase));
        files.Should().ContainSingle(f => f.Content!.Contains("FastAPI("));
    }
}
