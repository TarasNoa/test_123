using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifySubagentTests : IDisposable
{
    private readonly string _evidenceRoot;

    public VerifySubagentTests()
    {
        _evidenceRoot = Path.Combine(Path.GetTempPath(), $"verify-subagent-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_evidenceRoot))
                Directory.Delete(_evidenceRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task VerifyService_SkipsInBenchmarkMode()
    {
        var service = CreateService(benchmarkEnabled: true);
        var context = CreateContext(testsPassed: true);

        var result = await service.RunAsync(context);

        result.Skipped.Should().BeTrue();
        result.Passed.Should().BeTrue();
        result.SkipReason.Should().Be("benchmark_optional");
    }

    [Fact]
    public async Task VerifyService_FailsWhenTestsNotGreen()
    {
        var service = CreateService(benchmarkEnabled: false);
        var context = CreateContext(testsPassed: false);
        context.Orchestrator.MarkFailed("build_failed");

        var result = await service.RunAsync(context);

        result.Passed.Should().BeFalse();
        result.Summary.Should().Contain("tests not green");
    }

    [Fact]
    public async Task VerifyStage_StopsPipelineOnVerifyFailureInProduction()
    {
        var verify = new Mock<IVerifySubagentService>();
        verify.Setup(v => v.RunAsync(It.IsAny<GenerationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifySubagentResult(false, "verify failed", "/tmp/report.json"));

        var stage = new VerifyStage(
            verify.Object,
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            Options.Create(new VerifySubagentOptions { RequirePassInProduction = true }),
            NullLogger<VerifyStage>.Instance);

        var outcome = await stage.ExecuteAsync(CreateContext(testsPassed: true), CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Be("verify_not_passed");
    }

    [Fact]
    public async Task VerifyStage_ContinuesOnFailureInBenchmarkMode()
    {
        var verify = new Mock<IVerifySubagentService>();
        verify.Setup(v => v.RunAsync(It.IsAny<GenerationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifySubagentResult(false, "verify failed", "/tmp/report.json"));

        var stage = new VerifyStage(
            verify.Object,
            Options.Create(new AutonomousBenchmarkModeOptions
            {
                EnableBenchmarkMode = true,
                UseBenchmarkExecutionPath = true
            }),
            PlatformUtilizationTestOptions.BenchmarkShortcuts,
            Options.Create(new VerifySubagentOptions { RequirePassInProduction = true }),
            NullLogger<VerifyStage>.Instance);

        var outcome = await stage.ExecuteAsync(CreateContext(testsPassed: true), CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
    }

    private VerifySubagentService CreateService(bool benchmarkEnabled)
    {
        return new VerifySubagentService(
            new Mock<IVerifyRecipeRegistry>().Object,
            new Mock<IVerifyOrchestrator>().Object,
            new Mock<IVerifyGateService>().Object,
            new VerifyFailureContextStore(),
            new FileSystemVerifyEvidenceStore(
                Options.Create(new VerifySubagentOptions { EvidenceRoot = _evidenceRoot }),
                NullLogger<FileSystemVerifyEvidenceStore>.Instance),
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                EnableAgentSubagent = false,
                EvidenceRoot = _evidenceRoot
            }),
            Options.Create(new AutonomousBenchmarkModeOptions
            {
                EnableBenchmarkMode = benchmarkEnabled,
                UseBenchmarkExecutionPath = benchmarkEnabled
            }),
            benchmarkEnabled
                ? PlatformUtilizationTestOptions.BenchmarkShortcuts
                : PlatformUtilizationTestOptions.Production,
            NullLogger<VerifySubagentService>.Instance);
    }

    private static GenerationContext CreateContext(bool testsPassed)
    {
        var orchestrator = AppGenerationOrchestrator.Create(
            "build a calorie tracker",
            "fp-verify-test");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "calorie-app",
            applicationDescription: "calorie tracker",
            techStack: new TechStack(["Python"], ["Django"], [], [], "django stack"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12",
            buildCommands: ["python manage.py check"],
            testCommands: ["python manage.py test"],
            maxIterations: 3));
        if (testsPassed)
            orchestrator.MarkCompleted();

        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = "build a calorie tracker",
            Plan = orchestrator.Plan
        };
        if (testsPassed)
            context.Items["tests_passed"] = true;
        return context;
    }
}
