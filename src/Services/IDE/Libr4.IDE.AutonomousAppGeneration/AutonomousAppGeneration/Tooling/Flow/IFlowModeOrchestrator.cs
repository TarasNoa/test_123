namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public interface IFlowModeOrchestrator
{
    ExecutionMode SelectMode(string userRequest, int contextLength, int recentErrorCount);
}
