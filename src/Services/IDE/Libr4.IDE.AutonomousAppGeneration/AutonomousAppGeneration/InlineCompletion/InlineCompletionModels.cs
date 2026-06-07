namespace Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;

public sealed record InlineCompletionRequest(
    string FilePath,
    string Language,
    string FileContent,
    int Line,
    int Column,
    string? SessionIntent = null,
    string? WorkspaceHash = null,
    Guid? RunId = null,
    bool SuppressWhileAgentRunning = false);

public sealed record InlineCompletionResult(
    string? Text,
    bool Suppressed,
    string? SuppressReason,
    int LatencyMs,
    string? ModelUsed);
