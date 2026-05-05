namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public sealed class FlowModeOrchestrator : IFlowModeOrchestrator
{
    public ExecutionMode SelectMode(string userRequest, int contextLength, int recentErrorCount)
    {
        if (recentErrorCount >= 2)
            return ExecutionMode.Agent;

        var request = userRequest ?? string.Empty;
        var looksComplex = request.Length > 240 ||
                           request.Contains("production", StringComparison.OrdinalIgnoreCase) ||
                           request.Contains("architecture", StringComparison.OrdinalIgnoreCase);

        if (looksComplex || contextLength > 5000)
            return ExecutionMode.Flow;

        return ExecutionMode.Copilot;
    }
}
