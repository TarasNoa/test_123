using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed record AgentBackendDescriptor(
    AgentBackendKind Kind,
    IReadOnlyDictionary<string, string> Config)
{
    public static AgentBackendDescriptor Native { get; } =
        new(AgentBackendKind.Libr4Native, EmptyConfig);

    public static IReadOnlyDictionary<string, string> EmptyConfig { get; } =
        new Dictionary<string, string>();

    public static AgentBackendDescriptor Parse(string? backendSlug, IReadOnlyDictionary<string, string>? config = null)
    {
        var kind = backendSlug?.Trim().ToLowerInvariant() switch
        {
            null or "" or "libr4-native" or "libr4native" or "native" => AgentBackendKind.Libr4Native,
            "cursor-sdk" or "cursorsdk" => AgentBackendKind.CursorSdk,
            "codex-cli" or "codexcli" => AgentBackendKind.CodexCli,
            "opencode-cli" or "opencodecli" => AgentBackendKind.OpenCodeCli,
            "external-acp" or "acp" => AgentBackendKind.ExternalAcp,
            _ => throw new ArgumentException($"unknown_agent_backend:{backendSlug}", nameof(backendSlug))
        };

        return new AgentBackendDescriptor(kind, config ?? EmptyConfig);
    }
}

public sealed record AgentBackendSpawnRequest(
    Guid RunId,
    string Role,
    AgentBackendDescriptor Backend,
    AgentSessionRunRequest? SessionRequest = null,
    string? InitialMessage = null);

public sealed record AgentBackendHandle(
    string BackendInstanceId,
    Guid RunId,
    AgentBackendKind Kind,
    DateTime SpawnedAtUtc);

public enum AgentBackendRunStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record AgentBackendStatus(
    string BackendInstanceId,
    AgentBackendRunStatus Status,
    string? Stage,
    int? StepNumber,
    decimal? CostUsd,
    string? Error = null);

public enum AgentBackendEventKind
{
    Message,
    ToolUse,
    Status,
    Cost,
    Error
}

public sealed record AgentBackendEvent(
    AgentBackendEventKind Kind,
    Guid RunId,
    string BackendInstanceId,
    DateTime TimestampUtc,
    string PayloadJson);
