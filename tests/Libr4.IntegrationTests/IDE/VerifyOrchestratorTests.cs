using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifyOrchestratorTests
{
    [Fact]
    public void GateService_FailsWhenReadinessProbeFails()
    {
        var gate = new VerifyGateService();
        var plan = new VerifyRunPlan(
            Guid.NewGuid(),
            new VerifyRecipe(
                "django",
                "Python Django",
                [],
                [],
                [],
                ["cd backend && python manage.py runserver 0.0.0.0:8000"],
                [new VerifySmokeTarget("app", "http://localhost:8000/", 8000)],
                VerifySmokeKind.Http),
            "/tmp/verify",
            null,
            "python:3.12",
            null,
            true,
            "deterministic");

        var orchestration = new VerifyOrchestrationResult(
            ShadowPassed: true,
            ReadinessPassed: false,
            AgentPassed: true,
            AgentSummary: "ok",
            ReadinessResults:
            [
                new VerifyReadinessResult(
                    "app",
                    "http://localhost:8000/",
                    false,
                    [new VerifyReadinessAttempt("app", "http://localhost:8000/", 1, 0, false, "timeout", TimeSpan.FromSeconds(2))],
                    TimeSpan.FromSeconds(2))
            ],
            ReadinessEvidencePath: "/tmp/verify/readiness.json",
            FailureEvidencePath: "/tmp/verify/verify-failure-evidence.json");

        var result = gate.Evaluate(orchestration, plan);

        result.Passed.Should().BeFalse();
        result.FailureReasons.Should().Contain(r => r.StartsWith("readiness_failed:", StringComparison.Ordinal));
    }

    [Fact]
    public void GateService_PassesWhenAllChecksGreen()
    {
        var gate = new VerifyGateService();
        var plan = new VerifyRunPlan(
            Guid.NewGuid(),
            new VerifyRecipe("django", "Python Django", [], [], [], [], [], VerifySmokeKind.None),
            "/tmp/verify",
            null,
            "python:3.12",
            null,
            true,
            "deterministic");

        var orchestration = new VerifyOrchestrationResult(
            true,
            true,
            true,
            "ok",
            Array.Empty<VerifyReadinessResult>(),
            null,
            null);

        gate.Evaluate(orchestration, plan).Passed.Should().BeTrue();
    }

    [Fact]
    public void FailureContextStore_RetainsEvidenceForRepair()
    {
        var store = new VerifyFailureContextStore();
        var runId = Guid.NewGuid();
        var evidence = new VerifyFailureEvidence(
            runId,
            "banking",
            "verify gate failed",
            "shadow_verify=fail",
            "/tmp/readiness.json",
            "/tmp/verify-report.json",
            DateTime.UtcNow);

        store.Set(runId, evidence);
        store.TryGet(runId, out var loaded).Should().BeTrue();
        loaded!.RecipeId.Should().Be("banking");
        loaded.ReportText.Should().Contain("shadow_verify=fail");
    }
}
