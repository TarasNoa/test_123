using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultMcpSessionRouter : IMcpSessionRouter
{
    public McpExecutionLaneKind ResolveLane(McpToolMetadata tool, string? userRequestContext)
    {
        _ = userRequestContext;
        return tool.Lane;
    }
}
