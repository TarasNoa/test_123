using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public interface IComputerSubagentService
{
    Task<ComputerSubagentResult> RunAsync(
        AgentSpec spec,
        string task,
        ToolContext context,
        CancellationToken ct = default);
}
