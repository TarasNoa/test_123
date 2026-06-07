using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Full closed loop: verify pass → human review approved → PR created → CI webhook → fleet Completed.
/// </summary>
public sealed class FleetPrCiLoopTests : IDisposable
{
    private readonly string _root;

    public FleetPrCiLoopTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-pr-ci-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task FullLoop_VerifyReviewPrCi_CompletesOnFleetBoard()
    {
        var repo = new LoopStubRepository();
        var runId = repo.RunId;
        var registry = CreateRegistry(repo, out var shipSync, out var pullRequests);

        await registry.EnsureSchemaAsync();

        var orchestrator = await repo.GetAsync(runId);
        orchestrator!.RecordQualityGate("verify_subagent", 9, passed: true, ["obscura smoke passed"]);
        orchestrator.MarkCompleted();
        await repo.SaveAsync(orchestrator);

        await registry.UpsertFromRunAsync(runId);
        (await registry.GetSummaryAsync(runId))!.Entry.Status.Should().Be(AgentFleetStatus.Completed);

        var prResult = await pullRequests.CreatePrAsync(runId);
        prResult.Success.Should().BeTrue();
        prResult.PullRequestNumber.Should().Be(42);

        await registry.UpsertFromRunAsync(runId);
        var waiting = await registry.GetSummaryAsync(runId);
        waiting!.Entry.Status.Should().Be(AgentFleetStatus.WaitingForCi);
        waiting.Entry.PrUrl.Should().Contain("pull/42");
        waiting.Entry.CiStatus.Should().Be(FleetCiStatus.Pending);

        await shipSync.ApplyCiWebhookAsync(new GitHubCiWebhookPayload(
            "workflow_run",
            "completed",
            GitHubShipService.BuildHeadBranch(runId),
            "success",
            "https://github.com/org/repo/actions/runs/9001"));

        await registry.UpsertFromRunAsync(runId);
        var done = await registry.GetSummaryAsync(runId);
        done!.Entry.Status.Should().Be(AgentFleetStatus.Completed);
        done.Entry.CiStatus.Should().Be(FleetCiStatus.Success);
        done.Entry.CiLogsUrl.Should().Contain("actions/runs/9001");

        var list = await registry.ListAsync(new AgentFleetListQuery());
        list.Should().ContainSingle(i => i.RunId == runId && i.Status == AgentFleetStatus.Completed);
    }

    [Fact]
    public async Task CreatePr_BlockedUntilReviewApproved()
    {
        var repo = new LoopStubRepository();
        var runId = repo.RunId;
        var registry = CreateRegistry(repo, out _, out var pullRequests, reviewApproved: false);
        await registry.EnsureSchemaAsync();

        var blocked = await pullRequests.CreatePrAsync(runId);
        blocked.Success.Should().BeFalse();
        blocked.Summary.Should().Contain("human_review_pending");
    }

    private AgentFleetRegistry CreateRegistry(
        IAppGenerationRepository repository,
        out FleetShipSyncService shipSync,
        out PullRequestService pullRequests,
        bool reviewApproved = true)
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions { IndexDbPath = dbPath, RunsRoot = _root });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var shipState = new FleetShipStateStore(options);
        var flow = new Mock<IFlowProgressStore>();
        flow.Setup(x => x.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowProgress?)null);

        var fleetForward = new Mock<IAgentFleetRegistry>();
        shipSync = new FleetShipSyncService(
            shipState,
            new Lazy<IAgentFleetRegistry>(() => fleetForward.Object),
            NullLogger<FleetShipSyncService>.Instance);

        AgentFleetRegistry registry = new AgentFleetRegistry(
            index,
            repository,
            new AutonomousRunControlService(),
            flow.Object,
            options,
            NullLogger<AgentFleetRegistry>.Instance,
            shipState: shipState,
            shipSync: shipSync);

        fleetForward
            .Setup(x => x.UpsertFromRunAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>((id, ct) => registry.UpsertFromRunAsync(id, ct));

        var github = new GitHubShipService(
            Options.Create(new GitHubActionsDispatchOptions
            {
                Enabled = true,
                Owner = "org",
                Repository = "repo",
                PersonalAccessToken = "test-token",
                RequireVerifyPass = true,
                CreatePullRequest = true,
                DispatchWorkflow = false
            }),
            new StubGitHubApiClient(),
            NullLogger<GitHubShipService>.Instance);

        IReviewGate reviewGate = reviewApproved
            ? new ApprovedReviewGate()
            : new PendingReviewGate();

        pullRequests = new PullRequestService(
            repository,
            github,
            shipSync,
            NullLogger<PullRequestService>.Instance,
            reviewGate);

        return registry;
    }

    private sealed class ApprovedReviewGate : IReviewGate
    {
        public bool RequireHumanReview => true;
        public Task<bool> IsApprovedAsync(Guid runId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<RunReviewStatus> GetStatusAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult(RunReviewStatus.Approved);
    }

    private sealed class PendingReviewGate : IReviewGate
    {
        public bool RequireHumanReview => true;
        public Task<bool> IsApprovedAsync(Guid runId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<RunReviewStatus> GetStatusAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult(RunReviewStatus.Pending);
    }

    private sealed class StubGitHubApiClient : IGitHubApiClient
    {
        public Task DispatchWorkflowAsync(GitHubWorkflowDispatchRequest request, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<GitHubPullRequestCreateResult> CreatePullRequestWithFilesAsync(
            GitHubPullRequestRequest request,
            CancellationToken ct = default) =>
            Task.FromResult(new GitHubPullRequestCreateResult(
                42,
                "https://github.com/org/repo/pull/42",
                request.HeadBranch));

        public Task<string?> TryFetchWorkflowRunLogExcerptAsync(long runId, int maxChars, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task CreatePullRequestCommentAsync(
            GitHubRepositoryRef repository,
            int pullRequestNumber,
            string body,
            CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class LoopStubRepository : IAppGenerationRepository
    {
        private AppGenerationOrchestrator _run;

        public LoopStubRepository()
        {
            _run = AppGenerationOrchestrator.Create("build demo app", "fp-loop");
            _run.AttachPlan(new GenerationPlan(
                applicationName: "LoopApp",
                applicationDescription: "PR/CI loop test",
                techStack: new TechStack(["typescript"], ["react"], [], [], "react"),
                phases: Array.Empty<GenerationPhase>(),
                requiredAgents: Array.Empty<string>(),
                runtimeImage: "node:20",
                buildCommands: ["npm run build"],
                testCommands: ["npm test"],
                maxIterations: 3));
            _run.UpsertFile(new GeneratedFile("src/App.tsx", "typescript", "export {}"));
        }

        public Guid RunId => _run.Id;

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(id == _run.Id ? _run : null);

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_run);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
        {
            _run = orchestrator;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_run]);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            ListAsync(ct);
    }
}
