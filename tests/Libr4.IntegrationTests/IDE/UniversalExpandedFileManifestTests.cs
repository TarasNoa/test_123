using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class UniversalExpandedFileManifestTests
{
    [Fact]
    public void CreateRegistry_DjangoSolidJs_ReturnsPerFileTasks()
    {
        var plan = StrictStackContractEnforcer.Enforce(
            new GenerationPlan(
                "CalorieVision",
                "Django + SolidJS calorie app with backend/ and frontend/",
                new Libr4.IDE.Domain.AutonomousAppGeneration.TechStack(
                    new[] { "Python", "TypeScript" },
                    new[] { "Django", "SolidJS", "Vite" },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "test"),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "python:3.12-slim",
                new[] { "cd backend && python manage.py check", "cd frontend && npm run build" },
                Array.Empty<string>(),
                5),
            "Строго Django backend + SolidJS frontend (backend/ + frontend/).");

        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            UseExpandedJavaReactManifest = false,
            MaxFilesPerIncrementalTask = 1,
            UseFeatureScopedGeneration = false
        };

        var registry = MultiAgentIncrementalManifest.CreateRegistry(plan, options);
        registry.Should().NotBeNull();

        var backendTasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(
            AgentPhase.Backend, plan, options, registry);

        backendTasks.Should().HaveCountGreaterThan(8);
        backendTasks.Should().OnlyContain(t => t.Context.TargetRelativePaths.Length == 1);
        backendTasks.Should().OnlyContain(t => t.Context.ScopedOutputOnly);
        backendTasks.Select(t => t.Context.TargetRelativePaths[0])
            .Should()
            .Contain("backend/manage.py")
            .And.Contain("backend/meals/views.py")
            .And.Contain("backend/meals/tests.py")
            .And.Contain("backend/meals/exceptions.py");
    }

    [Fact]
    public void CreateRegistry_DotNet_ReturnsPerFileTasks()
    {
        var plan = new GenerationPlan(
            "InvoiceApi",
            "ASP.NET Core REST API",
            new Libr4.IDE.Domain.AutonomousAppGeneration.TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            MaxFilesPerIncrementalTask = 1,
            UseFeatureScopedGeneration = false
        };

        var registry = MultiAgentIncrementalManifest.CreateRegistry(plan, options);
        registry.Should().NotBeNull();
        var tasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(AgentPhase.Backend, plan, options, registry);
        tasks.Should().OnlyContain(t => t.Context.TargetRelativePaths.Length == 1);
        tasks.Should().HaveCountGreaterThan(3);
    }
}
