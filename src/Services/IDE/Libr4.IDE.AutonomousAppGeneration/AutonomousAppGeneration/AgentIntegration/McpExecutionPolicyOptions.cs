using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class McpExecutionPolicyOptions
{
    public McpToolRiskLevel MaxAllowedRisk { get; set; } = McpToolRiskLevel.High;

    public List<string> DeniedToolNames { get; set; } = new();

    public bool BlockPotentialPhiPatterns { get; set; } = true;
}
