using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ShipStageEvidenceGateTests : IDisposable
{
    private readonly string _runsRoot;

    public ShipStageEvidenceGateTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"ship-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_runsRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task ShipStage_BlocksWithoutObscuraEvidenceManifest()
    {
        var orchestrator = CreateOrchestrator();
        var gate = CreateEvidenceGate(CreateObscuraStore());
        var ship = CreateShipStage(gate);

        var outcome = await ship.ExecuteAsync(CreateContext(orchestrator, verifyPassed: true), CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Be("obscura_evidence_missing");
    }

    [Fact]
    public async Task ShipStage_ProceedsWhenObscuraManifestPresent()
    {
        var orchestrator = CreateOrchestrator();
        var store = CreateObscuraStore();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await store.PersistAsync(
            orchestrator.Id,
            ObscuraEvidenceKind.Screenshot,
            png,
            new ObscuraEvidencePersistOptions(LogicalName: "verify-shot", ToolName: "browser_screenshot"));

        var gate = CreateEvidenceGate(store);
        var ship = CreateShipStage(gate);

        var outcome = await ship.ExecuteAsync(CreateContext(orchestrator, verifyPassed: true), CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
    }

    [Fact]
    public async Task ShipStage_BlocksWhenVerifyNotPassed()
    {
        var orchestrator = CreateOrchestrator();
        var gate = CreateEvidenceGate(requireObscura: false);
        var ship = CreateShipStage(gate);

        var outcome = await ship.ExecuteAsync(CreateContext(orchestrator, verifyPassed: false), CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Be("verify_not_passed");
    }

    [Fact]
    public async Task ShipStage_BenchmarkMode_BypassesEvidenceGate()
    {
        var orchestrator = CreateOrchestrator();
        var gate = CreateEvidenceGate(benchmarkMode: true);
        var ship = CreateShipStage(gate);

        var outcome = await ship.ExecuteAsync(CreateContext(orchestrator, verifyPassed: false), CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
    }

    private ObscuraEvidenceShipGate CreateEvidenceGate(
        FileSystemObscuraEvidenceStore? store = null,
        bool requireObscura = true,
        bool benchmarkMode = false) =>
        new(
            Options.Create(new ShipStageOptions
            {
                RequireVerifyPass = true,
                RequireObscuraEvidenceManifest = requireObscura
            }),
            Options.Create(new AutonomousBenchmarkModeOptions
            {
                EnableBenchmarkMode = benchmarkMode,
                UseBenchmarkExecutionPath = benchmarkMode
            }),
            benchmarkMode
                ? PlatformUtilizationTestOptions.BenchmarkShortcuts
                : PlatformUtilizationTestOptions.Production,
            store);

    private FileSystemObscuraEvidenceStore CreateObscuraStore() =>
        new(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);

    private static AppGenerationOrchestrator CreateOrchestrator()
    {
        var orchestrator = AppGenerationOrchestrator.Create("ship gate test", Guid.NewGuid().ToString("N"));
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
        orchestrator.UpsertFile(new GeneratedFile("index.ts", "typescript", "export {}"));
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

    private static ShipStage CreateShipStage(IObscuraEvidenceShipGate evidenceGate)
    {
        var github = new GitHubShipService(
            Options.Create(new GitHubActionsDispatchOptions { Enabled = false }),
            new NoOpGitHubApiClient(),
            NullLogger<GitHubShipService>.Instance);

        return new ShipStage(
            github,
            Options.Create(new GitHubActionsDispatchOptions()),
            NullLogger<ShipStage>.Instance,
            evidenceGate: evidenceGate);
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
