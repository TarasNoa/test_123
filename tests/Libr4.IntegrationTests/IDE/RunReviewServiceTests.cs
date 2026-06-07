using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunReviewServiceTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly InMemoryAppGenerationRepository _repository = new();

    public RunReviewServiceTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"run-review-{Guid.NewGuid():N}");
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
    public async Task SubmitAsync_BatchApprove_SetsApprovedStatus()
    {
        var runId = await SeedOrchestratorAsync(["src/App.tsx", "src/main.ts"]);

        var service = CreateService(requireHumanReview: true);
        var status = await service.SubmitAsync(
            runId,
            new ReviewSubmissionRequest(
                ReviewDecision.Approve,
                ["src/App.tsx", "src/main.ts"]));

        status.Status.Should().Be(RunReviewStatus.Approved);
        status.ApprovedFiles.Should().Be(2);
        status.PendingPaths.Should().BeEmpty();
    }

    [Fact]
    public async Task ReviewGate_BlocksUntilAllFilesApproved()
    {
        var runId = await SeedOrchestratorAsync(["a.py", "b.py"]);
        var service = CreateService(requireHumanReview: true);
        var gate = new ReviewGate(service, Options.Create(new HumanReviewOptions { RequireHumanReview = true }));

        (await gate.IsApprovedAsync(runId)).Should().BeFalse();

        await service.SubmitAsync(runId, new ReviewSubmissionRequest(ReviewDecision.Approve, ["a.py"]));
        (await gate.IsApprovedAsync(runId)).Should().BeFalse();

        await service.SubmitAsync(runId, new ReviewSubmissionRequest(ReviewDecision.Approve, ["b.py"]));
        (await gate.IsApprovedAsync(runId)).Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_Reject_MarksRejectedAndWritesAuditTrail()
    {
        var runId = await SeedOrchestratorAsync(["secret.pem"]);
        var store = new FileRunReviewStore(
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            NullLogger<FileRunReviewStore>.Instance);
        var service = CreateService(requireHumanReview: true, store: store);

        var status = await service.SubmitAsync(
            runId,
            new ReviewSubmissionRequest(
                ReviewDecision.Reject,
                ["secret.pem"],
                Notes: "embedded key"));

        status.Status.Should().Be(RunReviewStatus.Rejected);
        var audit = await store.LoadAsync(runId);
        audit.Should().ContainSingle(e => e.Path == "secret.pem" && e.Decision == ReviewDecision.Reject);
        File.Exists(store.GetDecisionsPath(runId)).Should().BeTrue();
    }

    [Fact]
    public async Task GetStatus_WhenReviewNotRequired_ReturnsNotRequired()
    {
        var runId = await SeedOrchestratorAsync(["x.cs"]);
        var service = CreateService(requireHumanReview: false);

        var status = await service.GetStatusAsync(runId);

        status.Status.Should().Be(RunReviewStatus.NotRequired);
        status.RequireHumanReview.Should().BeFalse();
    }

    private RunReviewService CreateService(
        bool requireHumanReview,
        FileRunReviewStore? store = null)
    {
        store ??= new FileRunReviewStore(
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            NullLogger<FileRunReviewStore>.Instance);

        return new RunReviewService(
            store,
            Options.Create(new HumanReviewOptions
            {
                RequireHumanReview = requireHumanReview,
                AutoSpawnRepairOnReject = false
            }),
            NullLogger<RunReviewService>.Instance,
            _repository);
    }

    private async Task<Guid> SeedOrchestratorAsync(IReadOnlyList<string> paths)
    {
        var orchestrator = AppGenerationOrchestrator.Create("test app", "fp-review");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "test-app",
            applicationDescription: "desc",
            techStack: new TechStack(["typescript"], ["react"], [], [], "react"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "node:20",
            buildCommands: ["npm run build"],
            testCommands: ["npm test"],
            maxIterations: 3));

        foreach (var path in paths)
            orchestrator.UpsertFile(new GeneratedFile(path, "typescript", "// content"));

        await _repository.SaveAsync(orchestrator);
        return orchestrator.Id;
    }
}
