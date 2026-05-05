using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultMcpExecutionPolicy : IMcpExecutionPolicy
{
    private static readonly Regex PhiLoose = new(
        @"\b(ssn|social\s*security|credit\s*card|card\s*number|cvv|passport)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private readonly McpExecutionPolicyOptions _options;

    public DefaultMcpExecutionPolicy(IOptions<McpExecutionPolicyOptions> options)
    {
        _options = options.Value;
    }

    public McpPolicyDecision Evaluate(McpToolMetadata tool, string argumentsJson)
    {
        if (_options.DeniedToolNames.Any(x =>
                string.Equals(x, tool.ToolName, StringComparison.OrdinalIgnoreCase)))
            return new McpPolicyDecision(false, "tool_denied", "Tool is on the deny list");

        if (tool.Risk > _options.MaxAllowedRisk)
            return new McpPolicyDecision(false, "risk_too_high", $"Tool risk {tool.Risk} exceeds {_options.MaxAllowedRisk}");

        if (_options.BlockPotentialPhiPatterns && PhiLoose.IsMatch(argumentsJson))
            return new McpPolicyDecision(false, "phi_guard", "Arguments match PHI/PII keyword guard");

        return new McpPolicyDecision(true, "allowed", null);
    }
}
