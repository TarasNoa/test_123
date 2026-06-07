using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ShipStageReviewGateTests
{
    [Fact]
    public async Task ShipStage_BlocksWhenHumanReviewPending()
    {
        var (orchestrator, reviewService) = await CreateReviewContextAsync(["a.ts", "b.ts"]);
        var gate = new ReviewGate(reviewService, Options.Create(new HumanReviewOptions { RequireHumanReview = true }));
        var ship = CreateShipStage(gate);

        var outcome = await ship.ExecuteAsync(CreateContext(orchestrator, verifyPassed: true), CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Be("human_review_pending");
    }

    [Fact]
    public async Task ShipStage_ProceedsAfterBatchApprove()
    {
        var paths = new[] { "a.ts", "b.ts" };
        var (orchestrator, reviewService) = await CreateReviewContextAsync(paths);
        var gate = new ReviewGate(reviewService, Options.Create(new HumanReviewOptions { RequireHumanReview = true }));

        await reviewService.SubmitAsync(
            orchestrator.Id,
            new ReviewSubmissionRequest(ReviewDecision.Approve, paths));

        (await gate.IsApprovedAsync(orchestrator.Id)).Should().BeTrue();

        var outcome = await CreateShipStage(gate).ExecuteAsync(
            CreateContext(orchestrator, verifyPassed: true),
            CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
    }

    private static async Task<(AppGenerationOrchestrator Orchestrator, RunReviewService ReviewService)> CreateReviewContextAsync(
        IReadOnlyList<string> paths)
    {
        var repo = new InMemoryAppGenerationRepository();
        var orchestrator = CreateOrchestrator(paths);
        await repo.SaveAsync(orchestrator);

        var store = new FileRunReviewStore(
            Options.Create(new AgentRuntimeOptions { RunsRoot = Path.GetTempPath() }),
            NullLogger<FileRunReviewStore>.Instance);

        var reviewService = new RunReviewService(
            store,
            Options.Create(new HumanReviewOptions { RequireHumanReview = true, AutoSpawnRepairOnReject = false }),
            NullLogger<RunReviewService>.Instance,
            repo);

        return (orchestrator, reviewService);
    }

    private static AppGenerationOrchestrator CreateOrchestrator(IReadOnlyList<string> paths)
    {
        var orchestrator = AppGenerationOrchestrator.Create("ship test", Guid.NewGuid().ToString("N"));
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "app",
            applicationDescription: "desc",
            techStack: new TechStack(["typescript"], ["react"], [], [], "react"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "node:20",
            buildCommands: ["npm run build"],
            testCommands: ["npm test"],
            maxIterations: 3));
        foreach (var path in paths)
            orchestrator.UpsertFile(new GeneratedFile(path, "typescript", "// x"));
        return orchestrator;
    }

    private static GenerationContext CreateContext(AppGenerationOrchestrator orchestrator, bool verifyPassed)
    {
        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = orchestrator.UserRequest,
            Plan = orchestrator.Plan,
        };
        context.Items["verify_passed"] = verifyPassed;
        return context;
    }

    private static ShipStage CreateShipStage(IReviewGate gate)
    {
        var github = new GitHubShipService(
            Options.Create(new GitHubActionsDispatchOptions { Enabled = false }),
            new NoOpGitHubApiClient(),
            NullLogger<GitHubShipService>.Instance);

        return new ShipStage(
            github,
            Options.Create(new GitHubActionsDispatchOptions()),
            NullLogger<ShipStage>.Instance,
            gate);
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
