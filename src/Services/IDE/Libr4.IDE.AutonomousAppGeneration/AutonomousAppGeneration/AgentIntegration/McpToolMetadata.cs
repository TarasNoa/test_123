using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed record McpToolMetadata(
    string ToolName,
    string ServerProfileKey,
    string Description,
    McpToolRiskLevel Risk,
    McpExecutionLaneKind Lane,
    IReadOnlyList<string> Scopes);
