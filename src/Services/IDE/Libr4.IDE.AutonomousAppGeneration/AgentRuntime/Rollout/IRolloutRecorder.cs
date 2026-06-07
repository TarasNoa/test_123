namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;

public interface IRolloutRecorder
{
    Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default);
    Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default);
    Task RecordToolUseAsync(
        Guid runId,
        string sessionId,
        int stepNumber,
        string toolName,
        string inputJson,
        string outputJson,
        bool success,
        long durationMs,
        IReadOnlyList<RolloutMediaAttachment>? media = null,
        CancellationToken ct = default);
    Task RecordStepFinishAsync(
        Guid runId,
        string sessionId,
        int stepNumber,
        string finishReason,
        RolloutUsage? usage = null,
        CancellationToken ct = default);
    Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default);
    Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default);
    Task RecordSkillActivationAsync(
        Guid runId,
        string sessionId,
        string skillName,
        bool firstActivation,
        bool consentGranted,
        int contentChars,
        CancellationToken ct = default);
    Task RecordCompactionAsync(
        Guid runId,
        string sessionId,
        int beforeChars,
        int afterChars,
        int beforeTurns,
        int afterTurns,
        string summaryJson,
        CancellationToken ct = default);
    Task RecordMemoryOperationAsync(
        Guid runId,
        string sessionId,
        string operation,
        string scope,
        string? key,
        string? kind,
        int resultCount,
        CancellationToken ct = default);
    Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default);
}

public sealed record RolloutMediaAttachment(string Path, string? Url, string Kind);

public sealed record RolloutUsage(int? InputTokens, int? OutputTokens, int? TotalTokens, double? CostUsd);

public sealed record RolloutEntry(
    string Type,
    Guid RunId,
    string? SessionId,
    int StepNumber,
    DateTime TimestampUtc,
    string PayloadJson);

public sealed record RolloutSearchHit(Guid RunId, int StepNumber, string ToolName, string Snippet, double Score);

public interface IRolloutReplayService
{
    Task<IReadOnlyList<RolloutEntry>> ReplayAsync(Guid runId, CancellationToken ct = default);
}
