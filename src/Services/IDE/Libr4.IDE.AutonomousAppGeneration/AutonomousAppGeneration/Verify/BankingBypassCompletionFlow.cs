using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public enum BankingBypassCompletionOutcome
{
    NotApplicable,
    Completed,
    FailedVerify
}

/// <summary>
/// Phase 4.1: banking production-artifact bypass must run verify before MarkCompleted.
/// </summary>
public static class BankingBypassCompletionFlow
{
    public static async Task<BankingBypassCompletionOutcome> TryCompleteAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        string? userRequest,
        string qualityGateStage,
        int qualityGateScore,
        AutonomousLoopGuardOptions loopGuard,
        VerifySubagentOptions verifyOptions,
        Func<AppGenerationOrchestrator, GenerationPlan, string?, (bool Accepted, string Detail)> tryAcceptArtifacts,
        Func<AppGenerationOrchestrator, GenerationPlan, CancellationToken, Task<bool>> runVerify,
        Func<AppGenerationOrchestrator, string, IReadOnlyList<string>, CancellationToken, Task> onVerifyGateFailure,
        Func<AppGenerationOrchestrator, GenerationPlan, bool, CancellationToken, Task> onCompleted,
        CancellationToken ct)
    {
        if (!loopGuard.AllowBankingBypassWithoutGreenBuild)
            return BankingBypassCompletionOutcome.NotApplicable;

        var (accepted, detail) = tryAcceptArtifacts(orchestrator, plan, userRequest);
        if (!accepted)
            return BankingBypassCompletionOutcome.NotApplicable;

        orchestrator.RecordQualityGate(qualityGateStage, qualityGateScore, true, new[] { detail });

        var verifyPassed = await runVerify(orchestrator, plan, ct).ConfigureAwait(false);
        if (!verifyPassed && verifyOptions.RequirePassInProduction)
        {
            await onVerifyGateFailure(orchestrator, "verify", new[] { "verify_not_passed" }, ct)
                .ConfigureAwait(false);
            orchestrator.MarkFailed("verify_not_passed");
            return BankingBypassCompletionOutcome.FailedVerify;
        }

        orchestrator.MarkCompleted();
        await onCompleted(orchestrator, plan, verifyPassed, ct).ConfigureAwait(false);
        return BankingBypassCompletionOutcome.Completed;
    }
}
