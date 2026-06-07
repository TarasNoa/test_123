namespace Libr4.IDE.Application.AutonomousAppGeneration.DTOs;

public sealed record BuildDiagnosticsDashboardDto(
    Guid RunId,
    string Status,
    string? ApplicationName,
    string? FailureReason,
    DateTime GeneratedAtUtc,
    BuildDiagnosticsSummaryDto Summary,
    IReadOnlyList<BuildGateTimelineEntryDto> Timeline,
    IReadOnlyList<BuildPhaseDiagnosticsDto> Phases,
    IReadOnlyList<RepairTierDiagnosticsDto> RepairTiers,
    RecoveryEfficiencyReportDto RecoveryEfficiency,
    IReadOnlyList<string> Recommendations,
    VerifyEvidenceDiagnosticsDto? VerifyEvidence = null,
    ObscuraEvidenceDiagnosticsDto? ObscuraEvidence = null,
    VerifyRecipeDashboardDto? VerifyRecipe = null,
    IReadOnlyList<StackFilterOptionDto>? StackFilters = null,
    string? ActiveStackFilter = null);

public sealed record ObscuraEvidenceDiagnosticsDto(
    string ObscuraDirectory,
    string VerifyDirectory,
    bool DirectoryExists,
    string? ThumbnailUrl,
    string? ManifestUrl,
    IReadOnlyList<ObscuraEvidenceArtifactDto> Artifacts);

public sealed record ObscuraEvidenceArtifactDto(
    string Kind,
    string FileName,
    string ContentHash,
    string? LogicalName,
    int? StepNumber,
    string? ToolName,
    string DownloadUrl,
    string? ThumbnailUrl,
    long SizeBytes,
    DateTime LastModifiedUtc,
    string? ContentType);

public sealed record VerifyEvidenceDiagnosticsDto(
    string EvidenceDirectory,
    bool DirectoryExists,
    string? ThumbnailUrl,
    IReadOnlyList<VerifyEvidenceArtifactDto> Artifacts);

public sealed record VerifyEvidenceArtifactDto(
    string Kind,
    string FileName,
    string DownloadUrl,
    string? ThumbnailUrl,
    long SizeBytes,
    DateTime LastModifiedUtc,
    string? ContentType);

public sealed record RecoveryEfficiencyReportDto(
    int TotalAttempts,
    int ResolvedAttempts,
    int FailedAttempts,
    int PendingOutcome,
    int PatchesApplied,
    double LlmAttemptShare,
    double DeterministicAttemptShare,
    IReadOnlyList<RecoveryMechanismShareDto> ByMechanism,
    IReadOnlyList<RecoveryRootCauseShareDto> ByRootCause,
    IReadOnlyList<RecoverySourceShareDto> RecoverySource,
    FirstFailureReportDto? FirstFailure,
    string? PipelineStageReached,
    string? FirstFailureReason,
    string? LastFailureReason,
    bool RecoveryMeasurementEligible,
    string? RecoveryMeasurementSummary,
    LlmRecoveryStatsDto LlmStats,
    IReadOnlyList<RepeatedFailureDto> RepeatedErrors,
    IReadOnlyList<TimeLostByCategoryDto> TimeLostByRootCause,
    IReadOnlyList<RecoveryEfficiencyEventDto> Events,
    string Insight);

public sealed record RecoverySourceShareDto(
    string Source,
    int Attempts,
    double Share,
    int Resolved);

public sealed record FirstFailureReportDto(
    string ErrorClass,
    string RootCauseCategory,
    int Iteration,
    bool? Recovered);

public sealed record LlmRecoveryStatsDto(
    int Invoked,
    int Resolved,
    int Failed,
    double SuccessRate);

public sealed record RepeatedFailureDto(
    string Signature,
    int Count,
    int Resolved);

public sealed record TimeLostByCategoryDto(
    string RootCauseCategory,
    int Attempts,
    double TotalMinutes,
    double AvgMinutesPerAttempt);

public sealed record RecoveryMechanismShareDto(
    string Mechanism,
    int Attempts,
    double Share,
    int Resolved,
    int Failed);

public sealed record RecoveryRootCauseShareDto(
    string Category,
    int Attempts,
    double Share,
    int Resolved);

public sealed record RecoveryEfficiencyEventDto(
    int Iteration,
    string RootCauseCategory,
    string PrimaryErrorClass,
    string RecoveredBy,
    int PatchesApplied,
    bool? BuildSucceededAfterRepair,
    DateTime AttemptedAtUtc,
    string? ErrorSignature);

public sealed record DetectedEcosystemDto(
    string Id,
    string DisplayName,
    string Category,
    int MatchScore,
    IReadOnlyList<string> MatchReasons);

public sealed record BuildDiagnosticsSummaryDto(
    string DetectedStack,
    IReadOnlyList<DetectedEcosystemDto> DetectedEcosystems,
    int CatalogLanguageCount,
    int CatalogFrameworkCount,
    int TotalGates,
    int PassedGates,
    int FailedGates,
    double PassRate,
    int IterationCount,
    int FailedIterations,
    int FileCount,
    int OverallQualityScore,
    string QualityVerdict,
    string? WeakestPhase,
    string? StrongestPhase);

public sealed record BuildGateTimelineEntryDto(
    int Sequence,
    string Stage,
    string Category,
    string Tier,
    int Score,
    bool Passed,
    IReadOnlyList<string> Reasons,
    DateTime EvaluatedAtUtc);

public sealed record BuildPhaseDiagnosticsDto(
    string Category,
    int Evaluations,
    int Passed,
    int Failed,
    int LatestScore,
    double PassRate,
    IReadOnlyList<string> TopFailureReasons);

public sealed record RepairTierDiagnosticsDto(
    string Tier,
    int GateHits,
    int FailedGateHits,
    IReadOnlyList<string> Stages);

public sealed record VerifyRecipeDashboardDto(
    string RecipeId,
    string DisplayName,
    string DetectionMethod,
    IReadOnlyList<string> BuildCommands,
    IReadOnlyList<string> TestCommands,
    string SmokeKind);

public sealed record StackFilterOptionDto(
    string RecipeId,
    string DisplayName,
    int GateCount);
