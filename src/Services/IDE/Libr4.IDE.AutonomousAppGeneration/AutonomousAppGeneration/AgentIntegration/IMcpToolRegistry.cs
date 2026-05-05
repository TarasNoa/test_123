namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IMcpToolRegistry
{
    IReadOnlyList<McpToolMetadata> ListTools();

    McpToolMetadata? FindTool(string toolName);
}
