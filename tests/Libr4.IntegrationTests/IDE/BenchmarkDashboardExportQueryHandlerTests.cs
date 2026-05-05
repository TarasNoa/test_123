using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BenchmarkDashboardExportQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPersistDashboardSnapshotArtifact()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAppGenerationRepository, InMemoryAppGenerationRepository>();
        services.AddSingleton<IRunQualityAssessmentService, RunQualityAssessmentService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetBenchmarkDashboardQuery).Assembly));
        var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<IAppGenerationRepository>();
        var orchestrator = AppGenerationOrchestrator.Create("Generate app", "bench-export-fingerprint");
        var plan = new GenerationPlan(
            "SampleApp",
            "Sample",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "planning", "Plan", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "generation", "Generate", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "build", "Build", Array.Empty<AgentAssignment>())
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "pytest -q" },
            2);
        orchestrator.AttachPlan(plan);
        orchestrator.BeginGeneration();
        orchestrator.RecordQualityGate("generation", 9, true, Array.Empty<string>());
        orchestrator.MarkCompleted();
        await repository.SaveAsync(orchestrator);

        var mediator = provider.GetRequiredService<IMediator>();
        var tempExportDir = Path.Combine(Path.GetTempPath(), $"libr4-bench-test-{Guid.NewGuid():N}");
        var handler = new GetBenchmarkDashboardExportQueryHandler(
            mediator,
            Options.Create(new BenchmarkExportOptions
            {
                ExportRootPath = tempExportDir,
                RetentionHours = 1,
                MaxArtifacts = 50
            }));
        var result = await handler.Handle(new GetBenchmarkDashboardExportQuery(10), CancellationToken.None);

        result.ExportId.Should().NotBeNullOrWhiteSpace();
        result.ArtifactPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.ArtifactPath).Should().BeTrue();
        result.ArtifactPath.Should().StartWith(tempExportDir);
        result.ContentSha256.Should().HaveLength(64);
        result.Dashboard.TotalRuns.Should().BeGreaterThan(0);
    }
}
