using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IMcpToolInvocationService
{
    Task<McpInvocationOutcome> InvokeAsync(
        AppGenerationOrchestrator orchestrator,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct);

    /// <summary>
    /// Invoke MCP tool without attaching audit entries to a generation run (e.g. HTTP admin/tools).
    /// Uses <paramref name="userRequestContext"/> only for lane routing heuristics.
    /// </summary>
    Task<McpInvocationOutcome> InvokeStandaloneAsync(
        string? userRequestContext,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct);
}

public sealed record McpInvocationOutcome(
    bool Succeeded,
    string OutcomeCode,
    string? Detail,
    string? ResultSummary);
