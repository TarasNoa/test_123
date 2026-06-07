using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE.Pipeline;

/// <summary>
/// Phase 1.7: end-to-end pipeline via strangler-fig stages (pre-plan + post-plan runners).
/// </summary>
public sealed class FullPipelineStagesE2ETests
{
    [Fact]
    public async Task FullPipeline_PrePlanAndPostPlanStages_ExecuteInOrderAndComplete()
    {
        var context = BuildContext();
        var benchmarkOptions = Options.Create(new AutonomousBenchmarkModeOptions());

        var prePlanRunner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[]
            {
                new IdempotencyCheckStage(new EmptyRepository(), NullLogger<IdempotencyCheckStage>.Instance),
                new PlanGenerationStage(new StubPlanner(MakePlan()), NullLogger<PlanGenerationStage>.Instance),
                new PlanCommandValidationStage(
                    new DefaultPlanCommandValidator(),
                    benchmarkOptions,
                    NullLogger<PlanCommandValidationStage>.Instance),
                new PlanQualityGateStage(
                    new StubGateService(passed: true),
                    benchmarkOptions,
                    PlatformUtilizationTestOptions.Production,
                    NullLogger<PlanQualityGateStage>.Instance),
            },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);

        var prePlan = await prePlanRunner.RunAsync(context, CancellationToken.None);
        prePlan.Succeeded.Should().BeTrue();
        prePlan.ExecutedStageNames.Should().Equal(
            "idempotency_check",
            "plan_generation",
            "plan_command_validation",
            "plan_quality_gate");

        var postPlanRunner = CreatePostPlanRunner(benchmarkOptions);
        var postPlan = await postPlanRunner.RunPostPlanningAsync(context, CancellationToken.None);

        postPlan.Succeeded.Should().BeTrue();
        postPlan.ShortCircuited.Should().BeFalse();
        postPlan.ExecutedStageNames.Should().Equal(
            "generation",
            "security_review",
            "review_gate_2",
            "consistency_check",
            "startup_build",
            "repair_loop",
            "verify",
            "ship");

        context.Items["generation_stage_reached"].Should().Be(true);
        context.Items["security_review_stage_reached"].Should().Be(true);
        context.Items["review_gate_2_stage_reached"].Should().Be(true);
        context.Items["consistency_check_passed"].Should().Be(true);
        context.Items["startup_build_stage_reached"].Should().Be(true);
        context.Items["repair_loop_stage_reached"].Should().Be(true);
        context.Items["verify_stage_reached"].Should().Be(true);
        context.Items["ship_stage_reached"].Should().Be(true);
        context.Orchestrator.PipelineStageReached.Should().Be(AutonomousPipelineStages.Completed);
    }

    private static FullGenerationPipelineRunner CreatePostPlanRunner(
        IOptions<AutonomousBenchmarkModeOptions> benchmarkOptions)
    {
        var verify = new Mock<IVerifySubagentService>();
        verify.Setup(v => v.RunAsync(It.IsAny<GenerationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifySubagentResult(true, "verify passed", "/tmp/report.json"));

        var github = new GitHubShipService(
            Options.Create(new GitHubActionsDispatchOptions { Enabled = false }),
            new NoOpGitHubApiClient(),
            NullLogger<GitHubShipService>.Instance);

        return new FullGenerationPipelineRunner(
            new IGenerationStage[]
            {
                new GenerationStage(NullLogger<GenerationStage>.Instance),
                new SecurityReviewStage(),
                new ReviewGate2Stage(),
                new ConsistencyCheckStage(),
                new StartupBuildStage(),
                new RepairLoopStage(),
                new VerifyStage(
                    verify.Object,
                    benchmarkOptions,
                    PlatformUtilizationTestOptions.Production,
                    Options.Create(new VerifySubagentOptions { RequirePassInProduction = true }),
                    NullLogger<VerifyStage>.Instance),
                new ShipStage(
                    github,
                    Options.Create(new GitHubActionsDispatchOptions()),
                    NullLogger<ShipStage>.Instance),
            },
            NullLogger<FullGenerationPipelineRunner>.Instance);
    }

    private static GenerationContext BuildContext()
    {
        var orchestrator = AppGenerationOrchestrator.Create("full pipeline e2e", Guid.NewGuid().ToString("N"));
        orchestrator.UpsertFile(new GeneratedFile("Program.cs", "csharp", "class App {}"));

        return new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = orchestrator.UserRequest,
            Fingerprint = orchestrator.RequestFingerprint,
        };
    }

    private static GenerationPlan MakePlan(int maxIterations = 5) =>
        new GenerationPlan(
            "App",
            "Build something",
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            maxIterations);

    private sealed class EmptyRepository : IAppGenerationRepository
    {
        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(null);

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(null);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(Array.Empty<AppGenerationOrchestrator>());

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(Array.Empty<AppGenerationOrchestrator>());
    }

    private sealed class StubPlanner : IAppPlannerService
    {
        private readonly GenerationPlan _plan;
        public StubPlanner(GenerationPlan plan) => _plan = plan;

        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default) =>
            Task.FromResult(_plan);
    }

    private sealed class StubGateService : IAutonomousQualityGateService
    {
        private readonly bool _passed;
        public StubGateService(bool passed) => _passed = passed;

        public QualityGateResult EvaluatePlan(GenerationPlan plan) =>
            new("plan", _passed ? 10 : 3, _passed, Array.Empty<string>());

        public QualityGateResult EvaluateBuild(ExecutionResult execution) => throw new NotImplementedException();
        public QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan) => throw new NotImplementedException();
        public QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan) => throw new NotImplementedException();
        public QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches) => throw new NotImplementedException();
    }

    private sealed class NoOpGitHubApiClient : IGitHubApiClient
    {
        public Task DispatchWorkflowAsync(GitHubWorkflowDispatchRequest request, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<GitHubPullRequestCreateResult> CreatePullRequestWithFilesAsync(
            GitHubPullRequestRequest request,
            CancellationToken ct = default) =>
            Task.FromResult(new GitHubPullRequestCreateResult(42, "https://github.com/test/pr/42", "branch"));

        public Task<string?> TryFetchWorkflowRunLogExcerptAsync(long runId, int maxChars, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task CreatePullRequestCommentAsync(
            GitHubRepositoryRef repository,
            int pullRequestNumber,
            string body,
            CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
