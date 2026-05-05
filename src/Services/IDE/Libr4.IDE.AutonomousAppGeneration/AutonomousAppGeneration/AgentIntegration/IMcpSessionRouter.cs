using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IMcpSessionRouter
{
    McpExecutionLaneKind ResolveLane(McpToolMetadata tool, string? userRequestContext);
}
