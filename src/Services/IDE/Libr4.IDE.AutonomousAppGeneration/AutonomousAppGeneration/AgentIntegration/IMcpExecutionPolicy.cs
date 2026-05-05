using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IMcpExecutionPolicy
{
    McpPolicyDecision Evaluate(McpToolMetadata tool, string argumentsJson);
}

public sealed record McpPolicyDecision(bool Allowed, string Code, string? Detail);
