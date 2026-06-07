using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BankingBypassCompletionFlowTests
{
    [Fact]
    public async Task VerifyFail_BlocksCompletion_WhenRequirePassInProduction()
    {
        var orchestrator = CreateOrchestrator();
        var plan = orchestrator.Plan!;
        var loopGuard = new AutonomousLoopGuardOptions { AllowBankingBypassWithoutGreenBuild = true };
        var verifyOptions = new VerifySubagentOptions { RequirePassInProduction = true };
        var completedHookCalled = false;

        var outcome = await BankingBypassCompletionFlow.TryCompleteAsync(
            orchestrator,
            plan,
            userRequest: "banking spring boot react",
            qualityGateStage: "fix_deferred_shadow_build",
            qualityGateScore: 8,
            loopGuard,
            verifyOptions,
            (_, _, _) => (true, "production_score=9;shadow_build_unresolved"),
            (_, _, _) => Task.FromResult(false),
            (_, _, _, _) => Task.CompletedTask,
            (_, _, _, _) =>
            {
                completedHookCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        outcome.Should().Be(BankingBypassCompletionOutcome.FailedVerify);
        orchestrator.Status.Should().Be(GenerationStatus.Failed);
        orchestrator.FailureReason.Should().Be("verify_not_passed");
        completedHookCalled.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPass_CompletesRun_AndInvokesPostCompleteHook()
    {
        var orchestrator = CreateOrchestrator();
        var plan = orchestrator.Plan!;
        var loopGuard = new AutonomousLoopGuardOptions { AllowBankingBypassWithoutGreenBuild = true };
        var verifyOptions = new VerifySubagentOptions { RequirePassInProduction = true };
        var completedHookCalled = false;

        var outcome = await BankingBypassCompletionFlow.TryCompleteAsync(
            orchestrator,
            plan,
            userRequest: "banking spring boot react",
            qualityGateStage: "iteration_budget_banking_accept",
            qualityGateScore: 8,
            loopGuard,
            verifyOptions,
            (_, _, _) => (true, "production_score=9;shadow_build_unresolved"),
            (_, _, _) => Task.FromResult(true),
            (_, _, _, _) => Task.CompletedTask,
            (_, _, verifyPassed, _) =>
            {
                completedHookCalled = true;
                verifyPassed.Should().BeTrue();
                return Task.CompletedTask;
            },
            CancellationToken.None);

        outcome.Should().Be(BankingBypassCompletionOutcome.Completed);
        orchestrator.Status.Should().Be(GenerationStatus.Completed);
        completedHookCalled.Should().BeTrue();
        orchestrator.QualityGates.Should().Contain(g =>
            g.Stage == "iteration_budget_banking_accept" && g.Passed);
    }

    [Fact]
    public async Task ArtifactsNotAccepted_ReturnsNotApplicable()
    {
        var orchestrator = CreateOrchestrator();
        var plan = orchestrator.Plan!;
        var loopGuard = new AutonomousLoopGuardOptions { AllowBankingBypassWithoutGreenBuild = true };

        var outcome = await BankingBypassCompletionFlow.TryCompleteAsync(
            orchestrator,
            plan,
            userRequest: "banking",
            qualityGateStage: "fix_deferred_shadow_build",
            qualityGateScore: 8,
            loopGuard,
            new VerifySubagentOptions(),
            (_, _, _) => (false, string.Empty),
            (_, _, _) => Task.FromResult(true),
            (_, _, _, _) => Task.CompletedTask,
            (_, _, _, _) => Task.CompletedTask,
            CancellationToken.None);

        outcome.Should().Be(BankingBypassCompletionOutcome.NotApplicable);
        orchestrator.Status.Should().NotBe(GenerationStatus.Completed);
    }

    private static AppGenerationOrchestrator CreateOrchestrator()
    {
        var orchestrator = AppGenerationOrchestrator.Create(
            "build banking spring boot react app",
            "fp-banking-bypass-verify");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "BankingPortal",
            applicationDescription: "Banking portal spring boot + react",
            techStack: new TechStack(["Java", "TypeScript"], ["Spring Boot", "React"], [], [], "spring+react"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "eclipse-temurin:21",
            buildCommands: ["cd backend && mvn -B -ntp -DskipTests package"],
            testCommands: ["cd backend && mvn -B -ntp test"],
            maxIterations: 3));
        return orchestrator;
    }
}
