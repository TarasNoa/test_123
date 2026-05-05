using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE.Pipeline;

public sealed class IndividualStagesTests
{
    // ---- IdempotencyCheckStage ------------------------------------------------

    [Fact]
    public async Task IdempotencyStage_NoFingerprint_Continues()
    {
        var stage = new IdempotencyCheckStage(new StubRepository(null), NullLogger<IdempotencyCheckStage>.Instance);
        var ctx = MakeContext(fingerprint: null);

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        outcome.ShortCircuit.Should().BeFalse();
        ctx.ShortCircuitOrchestrator.Should().BeNull();
    }

    [Fact]
    public async Task IdempotencyStage_FingerprintMissing_Continues()
    {
        var stage = new IdempotencyCheckStage(new StubRepository(null), NullLogger<IdempotencyCheckStage>.Instance);
        var ctx = MakeContext(fingerprint: "fp-1");

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        outcome.ShortCircuit.Should().BeFalse();
    }

    [Fact]
    public async Task IdempotencyStage_FailedRunFound_DoesNotShortCircuit()
    {
        var prior = AppGenerationOrchestrator.Create("prior", "fp-1");
        prior.MarkFailed("prior_failed");
        var stage = new IdempotencyCheckStage(new StubRepository(prior), NullLogger<IdempotencyCheckStage>.Instance);
        var ctx = MakeContext(fingerprint: "fp-1");

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.ShortCircuitOrchestrator.Should().BeNull();
    }

    [Fact]
    public async Task IdempotencyStage_CompletedRunFound_ShortCircuitsAndAttachesOrchestrator()
    {
        // Audit § 4.2: only genuinely Completed runs short-circuit; in-progress/Planning/
        // Failed/Cancelled runs are re-executed. The stage previously reused any non-Failed
        // run which incorrectly returned "success" for cancelled prior runs.
        var prior = AppGenerationOrchestrator.Create("prior", "fp-1");
        prior.AttachPlan(new GenerationPlan(
            applicationName: "prior",
            applicationDescription: "test",
            techStack: new TechStack(new[] { "C#" }, new[] { "ASP.NET" }, Array.Empty<string>(), Array.Empty<string>(), "test"),
            phases: new[] { new GenerationPhase(1, "p", "p", Array.Empty<AgentAssignment>()) },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "img",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 3));
        prior.BeginGeneration();
        prior.MarkCompleted();
        var stage = new IdempotencyCheckStage(new StubRepository(prior), NullLogger<IdempotencyCheckStage>.Instance);
        var ctx = MakeContext(fingerprint: "fp-1");

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShortCircuit.Should().BeTrue();
        ctx.ShortCircuitOrchestrator.Should().BeSameAs(prior);
    }

    [Fact]
    public async Task IdempotencyStage_InProgressRunFound_DoesNotShortCircuit()
    {
        // Audit § 4.2: a Planning/Generating prior run must NOT be reused — re-executing
        // is the safe default when status is non-terminal.
        var prior = AppGenerationOrchestrator.Create("prior", "fp-1");
        var stage = new IdempotencyCheckStage(new StubRepository(prior), NullLogger<IdempotencyCheckStage>.Instance);
        var ctx = MakeContext(fingerprint: "fp-1");

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.ShortCircuitOrchestrator.Should().BeNull();
    }

    // ---- PlanGenerationStage --------------------------------------------------

    [Fact]
    public async Task PlanGenerationStage_NoExistingPlan_InvokesPlannerAndCapsIterations()
    {
        var planner = new StubPlanner(MakePlan(maxIterations: 30));
        var stage = new PlanGenerationStage(planner, NullLogger<PlanGenerationStage>.Instance);
        var ctx = MakeContext(requestedMaxIterations: 5);

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.Plan.Should().NotBeNull();
        ctx.Plan!.MaxIterations.Should().Be(5, "request budget is tighter than plan default");
        planner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task PlanGenerationStage_ExistingPlan_SkipsPlanner()
    {
        var existing = MakePlan(maxIterations: 10);
        var planner = new StubPlanner(MakePlan(maxIterations: 30));
        var stage = new PlanGenerationStage(planner, NullLogger<PlanGenerationStage>.Instance);
        var ctx = MakeContext();
        ctx.Plan = existing;

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.Plan.Should().BeSameAs(existing);
        planner.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task PlanGenerationStage_PlannerThrows_StopsWithFailureReason()
    {
        var stage = new PlanGenerationStage(new ThrowingPlanner(), NullLogger<PlanGenerationStage>.Instance);
        var ctx = MakeContext();

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().StartWith("plan_generation_failed:InvalidOperationException");
    }

    // ---- PlanQualityGateStage -------------------------------------------------

    [Fact]
    public async Task PlanQualityGateStage_NoPlan_StopsWithMissingPlan()
    {
        var stage = new PlanQualityGateStage(new StubGateService(passed: true), NullLogger<PlanQualityGateStage>.Instance);
        var ctx = MakeContext();

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Be("plan_missing_for_quality_gate");
    }

    [Fact]
    public async Task PlanQualityGateStage_GatePasses_RecordsAndContinues()
    {
        var gates = new StubGateService(passed: true);
        var stage = new PlanQualityGateStage(gates, NullLogger<PlanQualityGateStage>.Instance);
        var ctx = MakeContext();
        ctx.Plan = MakePlan(maxIterations: 10);

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        ctx.Orchestrator.QualityGates.Should().ContainSingle(g => g.Stage == "plan");
    }

    [Fact]
    public async Task PlanQualityGateStage_GateFails_StopsWithReason()
    {
        var gates = new StubGateService(passed: false, reasons: new[] { "missing_phases", "no_runtime" });
        var stage = new PlanQualityGateStage(gates, NullLogger<PlanQualityGateStage>.Instance);
        var ctx = MakeContext();
        ctx.Plan = MakePlan(maxIterations: 10);

        var outcome = await stage.ExecuteAsync(ctx, CancellationToken.None);

        outcome.ShouldContinue.Should().BeFalse();
        outcome.FailureReason.Should().Contain("quality_gate_plan_failed");
        outcome.FailureReason.Should().Contain("missing_phases");
        ctx.Orchestrator.QualityGates.Should().ContainSingle(g => g.Stage == "plan" && !g.Passed);
    }

    // ---- helpers --------------------------------------------------------------

    private static GenerationContext MakeContext(string? fingerprint = "fp-test", int requestedMaxIterations = 0)
    {
        var orch = AppGenerationOrchestrator.Create("test request", fingerprint ?? "fp-test");
        return new GenerationContext
        {
            Orchestrator = orch,
            UserRequest = "test request",
            Fingerprint = fingerprint,
            RequestedMaxIterations = requestedMaxIterations
        };
    }

    private static GenerationPlan MakePlan(int maxIterations = 10) =>
        new GenerationPlan(
            "App", "Build something",
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            maxIterations);

    private sealed class StubRepository : IAppGenerationRepository
    {
        private readonly AppGenerationOrchestrator? _byFingerprint;
        public StubRepository(AppGenerationOrchestrator? byFingerprint) { _byFingerprint = byFingerprint; }
        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult<AppGenerationOrchestrator?>(null);
        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) => Task.FromResult(_byFingerprint);
        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(Array.Empty<AppGenerationOrchestrator>());
        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(Array.Empty<AppGenerationOrchestrator>());
    }

    private sealed class StubPlanner : IAppPlannerService
    {
        private readonly GenerationPlan _plan;
        public int CallCount { get; private set; }
        public StubPlanner(GenerationPlan plan) { _plan = plan; }
        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(_plan);
        }
    }

    private sealed class ThrowingPlanner : IAppPlannerService
    {
        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
            => throw new InvalidOperationException("planner_simulated_failure");
    }

    private sealed class StubGateService : IAutonomousQualityGateService
    {
        private readonly bool _passed;
        private readonly IReadOnlyList<string> _reasons;
        public StubGateService(bool passed, IReadOnlyList<string>? reasons = null)
        { _passed = passed; _reasons = reasons ?? Array.Empty<string>(); }

        public QualityGateResult EvaluatePlan(GenerationPlan plan) => new("plan", _passed ? 10 : 3, _passed, _reasons);
        public QualityGateResult EvaluateBuild(ExecutionResult execution) => throw new NotImplementedException();
        public QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan) => throw new NotImplementedException();
        public QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan) => throw new NotImplementedException();
        public QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches) => throw new NotImplementedException();
    }
}
