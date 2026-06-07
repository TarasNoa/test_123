namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed record VerifyRunPlan(
    Guid RunId,
    VerifyRecipe Recipe,
    string EvidenceDir,
    Guid? ShadowWorkspaceId,
    string RuntimeImage,
    string? ManifestPath,
    bool TestsGreen,
    string DetectionMethod);

public sealed record VerifyReadinessAttempt(
    string TargetName,
    string Url,
    int Attempt,
    int StatusCode,
    bool Ready,
    string? Error,
    TimeSpan Elapsed);

public sealed record VerifyReadinessResult(
    string TargetName,
    string Url,
    bool Ready,
    IReadOnlyList<VerifyReadinessAttempt> Attempts,
    TimeSpan TotalElapsed);

public sealed record VerifyOrchestrationResult(
    bool ShadowPassed,
    bool ReadinessPassed,
    bool AgentPassed,
    string AgentSummary,
    IReadOnlyList<VerifyReadinessResult> ReadinessResults,
    string? ReadinessEvidencePath,
    string? FailureEvidencePath);

public sealed record VerifyGateResult(
    bool Passed,
    string Summary,
    IReadOnlyList<string> FailureReasons);

public sealed record VerifyFailureEvidence(
    Guid RunId,
    string RecipeId,
    string Summary,
    string ReportText,
    string? ReadinessEvidencePath,
    string? VerifyReportPath,
    DateTime CapturedAtUtc);
