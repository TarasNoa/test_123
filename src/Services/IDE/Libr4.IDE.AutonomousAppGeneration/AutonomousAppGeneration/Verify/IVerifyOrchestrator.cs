using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IVerifyOrchestrator
{
    VerifyRunPlan PrepareVerifyRun(
        GenerationContext context,
        VerifyRecipeDetectionResult recipeDetection,
        string evidenceDir);

    Task<VerifyOrchestrationResult> RunVerifyOrchestrationAsync(
        GenerationContext context,
        VerifyRunPlan plan,
        CancellationToken ct = default);
}

public interface IVerifyReadinessProbe
{
    Task<VerifyReadinessResult> ProbeAsync(
        VerifySmokeTarget target,
        Guid? shadowWorkspaceId,
        string evidenceDir,
        Guid? runId = null,
        CancellationToken ct = default);
}

public interface IVerifyGateService
{
    VerifyGateResult Evaluate(VerifyOrchestrationResult orchestration, VerifyRunPlan plan);
}

public interface IVerifyFailureContextStore
{
    void Set(Guid runId, VerifyFailureEvidence evidence);

    bool TryGet(Guid runId, out VerifyFailureEvidence? evidence);
}
