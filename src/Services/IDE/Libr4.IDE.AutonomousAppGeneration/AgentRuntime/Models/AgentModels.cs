using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

public enum AgentTurnAction
{
    Tool,
    Done,
    Invalid
}

public sealed record AgentToolCall(string Name, JsonElement Input);

public sealed record AgentTurnResponse(
    AgentTurnAction Action,
    AgentToolCall? ToolCall,
    string? Summary,
    string Raw,
    string? Reasoning = null);

public sealed record AgentConversationTurn(
    string Role,
    string Content,
    DateTime AtUtc);

public sealed record ToolExecutionResult(
    string ToolName,
    bool Success,
    string Output,
    IReadOnlyList<GeneratedFile> FilePatches);

public sealed record AgentSessionResult(
    bool Succeeded,
    string Summary,
    IReadOnlyList<GeneratedFile> Patches,
    int TurnsUsed,
    IReadOnlyList<string> Trace);

public sealed record ShadowWorkspaceContext(
    Guid WorkspaceId,
    string HostPath,
    string GuestSubdir,
    Runtime.IRuntimeSession Runtime);
