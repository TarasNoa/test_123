namespace Libr4.IDE.Application.AutonomousAppGeneration.DTOs;

public sealed record AppGenerationResponse(
    Guid Id,
    string Status,
    string ApplicationName,
    int Iterations,
    int MaxIterations,
    bool Succeeded,
    string? FailureReason);

public sealed record IterationDto(
    Guid Id,
    int Number,
    bool Succeeded,
    int ErrorCount,
    IReadOnlyList<string> AppliedFixes,
    int RetryCount,
    IReadOnlyList<RetryEventDto> RetryEvents,
    DateTime StartedAt,
    DateTime? CompletedAt);

public sealed record RetryEventDto(
    int Attempt,
    string Reason,
    long BackoffMs,
    DateTime TimestampUtc);

public sealed record CommandExecutionDto(
    string Phase,
    string Command,
    int ExitCode,
    long DurationMs,
    string RuntimeProvider,
    string RuntimeSessionId,
    DateTime ExecutedAtUtc);

public sealed record QualityGateResultDto(
    string Stage,
    int Score,
    bool Passed,
    IReadOnlyList<string> Reasons,
    DateTime EvaluatedAtUtc);

public sealed record StageQualityScoreDto(
    string Stage,
    int LatestScore,
    double AverageScore,
    int Evaluations,
    bool LastPassed);

public sealed record RunQualityAssessmentDto(
    int OverallScore,
    string Verdict,
    IReadOnlyList<StageQualityScoreDto> StageScores);

public sealed record McpExecutionDto(
    string ToolName,
    string ServerName,
    string Lane,
    string RiskLevel,
    string ArgumentsSha256,
    DateTime StartedAtUtc,
    long DurationMs,
    string Outcome,
    string? Detail);

public sealed record MemoryIngestDto(
    Guid RunId,
    string Stage,
    string Kind,
    string Key,
    string Summary,
    int TokenEstimate,
    DateTime StoredAtUtc);

public sealed record MemoryRetrievalDto(
    Guid RunId,
    string Stage,
    string Kind,
    string Key,
    string Summary,
    string RetrievalReason,
    double RelevanceScore,
    DateTime RetrievedAtUtc);

public sealed record SkillInvocationDto(
    string SkillId,
    string Version,
    string Stage,
    string SafetyLabel,
    DateTime StartedAtUtc,
    long DurationMs,
    string Outcome,
    string? Detail);

public sealed record TaskGraphEntryDto(
    string TaskId,
    string Title,
    IReadOnlyList<string> BlockedByTaskIds,
    string State,
    IReadOnlyList<string> EvidencePaths,
    string? Notes);

public sealed record SecurityReviewDto(
    string Stage,
    int Score,
    bool Passed,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RemediationHints,
    DateTime EvaluatedAtUtc);

public sealed record CascadePlanTraceDto(
    string Rationale,
    string SerializedPlanJson,
    int PhaseCount,
    string RoutingProfile,
    string? ModelHint,
    string PlannerMode,
    DateTime CreatedAtUtc);

public sealed record CheckpointAuditDto(
    string CheckpointId,
    string Label,
    string Action,
    int FileCount,
    int ChangedFiles,
    string? Detail,
    DateTime CreatedAtUtc);

public sealed record TriggerIngestionDto(
    string Source,
    string AdapterName,
    string NormalizedRequest,
    string? Actor,
    string? CorrelationId,
    DateTime ReceivedAtUtc);

public sealed record BenchmarkStageSummaryDto(
    string Stage,
    int Evaluations,
    int Passed,
    int Failed,
    int AvgScore,
    long AvgDurationMs);

public sealed record BenchmarkSummaryDto(
    int TotalQualityEvaluations,
    int TotalFailedEvaluations,
    long TotalCommandDurationMs,
    long AvgCommandDurationMs,
    IReadOnlyList<string> TopFailureReasons,
    IReadOnlyList<BenchmarkStageSummaryDto> Stages);

public sealed record ExecutionManifestDto(
    string SchemaVersion,
    string ManifestId,
    string ContentSha256,
    string? ArtifactPath,
    Guid OrchestratorId,
    string UserRequest,
    string RequestFingerprint,
    string FinalStatus,
    DateTime GeneratedAtUtc,
    int IterationCount,
    int TotalCommands,
    int TotalRetries,
    IReadOnlyList<RetryEventDto> RetryEvents,
    IReadOnlyList<QualityGateResultDto> QualityGates,
    RunQualityAssessmentDto QualityAssessment,
    IReadOnlyList<CommandExecutionDto> Commands,
    IReadOnlyList<McpExecutionDto> McpExecutions,
    IReadOnlyList<MemoryIngestDto> MemoryIngests,
    IReadOnlyList<MemoryRetrievalDto> MemoryRetrievals,
    IReadOnlyList<SkillInvocationDto> SkillInvocations,
    IReadOnlyList<TaskGraphEntryDto> TaskGraph,
    IReadOnlyList<SecurityReviewDto> SecurityReviews,
    CascadePlanTraceDto? CascadePlan,
    IReadOnlyList<CheckpointAuditDto> Checkpoints,
    IReadOnlyList<TriggerIngestionDto> Triggers,
    BenchmarkSummaryDto BenchmarkSummary,
    IReadOnlyList<McpLaneWatchdogSnapshotDto> McpLaneWatchdogSnapshots,
    IReadOnlyList<string> RunRemediationHints,
    IReadOnlyList<RecoveryTraceDto> RecoveryTrace);

public sealed record RecoveryTraceDto(
    string StrategyName,
    string Reason,
    DateTime TimestampUtc,
    long DurationMs,
    bool Success,
    string? ContextSnapshot);

public sealed record ErrorReportDto(
    string ErrorType,
    string Message,
    string? FilePath,
    int? LineNumber,
    string SuggestedFix,
    string? DiagnosingAgent);

public sealed record GeneratedFileDto(
    string RelativePath,
    string Language,
    string Content,
    DateTime UpdatedAt);

public sealed record TechStackDto(
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Frameworks,
    IReadOnlyList<string> Databases,
    IReadOnlyList<string> Infrastructure,
    string Rationale);

public sealed record AgentAssignmentDto(string AgentName, string Role, string TaskDescription);

public sealed record GenerationPhaseDto(
    int Order,
    string Name,
    string Description,
    IReadOnlyList<AgentAssignmentDto> Assignments);

public sealed record GenerationPlanDto(
    string ApplicationName,
    string ApplicationDescription,
    TechStackDto TechStack,
    IReadOnlyList<GenerationPhaseDto> Phases,
    IReadOnlyList<string> RequiredAgents,
    string RuntimeImage,
    IReadOnlyList<string> BuildCommands,
    IReadOnlyList<string> TestCommands,
    int MaxIterations);

public sealed record AppGenerationReportDto(
    Guid Id,
    string Status,
    string? FailureReason,
    /// <summary>Convenience mirror of <see cref="Plan"/>.ApplicationName while polling long runs.</summary>
    string? ApplicationName,
    /// <summary>Count of generated files currently stored on the run.</summary>
    int FileCount,
    GenerationPlanDto? Plan,
    IReadOnlyList<QualityGateResultDto> QualityGates,
    RunQualityAssessmentDto QualityAssessment,
    IReadOnlyList<IterationDto> Iterations,
    IReadOnlyList<GeneratedFileDto> Files,
    IReadOnlyList<ErrorReportDto> OutstandingErrors,
    ExecutionManifestDto Manifest,
    BenchmarkSummaryDto BenchmarkSummary,
    IReadOnlyList<MemoryRetrievalDto> MemoryRetrievals,
    CascadePlanTraceDto? CascadePlan,
    DateTime StartedAt,
    DateTime? CompletedAt);

public sealed record BenchmarkRunPointDto(
    Guid RunId,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int OverallScore,
    int FailedQualityGates,
    long TotalCommandDurationMs);

public sealed record BenchmarkStageTrendDto(
    string Stage,
    int Evaluations,
    double AverageScore,
    double PassRate,
    long AverageDurationMs);

public sealed record BenchmarkRegressionDto(
    string Stage,
    double BaselineAverageScore,
    int LatestScore,
    double Delta,
    IReadOnlyList<string> LatestFailureReasons);

public sealed record BenchmarkDashboardDto(
    DateTime GeneratedAtUtc,
    int TotalRuns,
    int SucceededRuns,
    int FailedRuns,
    double SuccessRate,
    int TotalMcpDegradedEvents,
    IReadOnlyList<string> TopMcpBlockerCodes,
    IReadOnlyList<string> TopFailureReasons,
    IReadOnlyList<BenchmarkStageTrendDto> StageTrends,
    IReadOnlyList<BenchmarkRegressionDto> TopRegressions,
    IReadOnlyList<BenchmarkRunPointDto> Runs);

public sealed record BenchmarkDashboardExportDto(
    string ExportId,
    string ContentSha256,
    string ArtifactPath,
    DateTime GeneratedAtUtc,
    BenchmarkDashboardDto Dashboard);

public sealed record StageCReadinessItemDto(
    string ProfileKey,
    string Lane,
    string Status,
    string? BlockerCode,
    string? DiagnosticMessage,
    bool KillSwitchActive,
    IReadOnlyList<string> RemediationHints);

public sealed record StageCReadinessDto(
    DateTime GeneratedAtUtc,
    bool DeterministicFallbackEnabled,
    bool StdioTransportEnabled,
    int TotalProfiles,
    int DegradedProfiles,
    string OverallStatus,
    IReadOnlyList<string> OverallRecommendations,
    IReadOnlyList<StageCReadinessItemDto> Items);
