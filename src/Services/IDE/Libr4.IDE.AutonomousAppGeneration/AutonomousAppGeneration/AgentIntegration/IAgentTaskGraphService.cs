using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IAgentTaskGraphService
{
    IReadOnlyList<AgentTaskGraphEntry> BuildInitial(GenerationPlan plan, string userRequest);

    IReadOnlyList<AgentTaskGraphEntry> Transition(
        IReadOnlyList<AgentTaskGraphEntry> current,
        string taskId,
        AgentTaskState newState,
        IReadOnlyList<string>? evidencePaths = null,
        string? notes = null);

    IReadOnlyList<AgentTaskGraphEntry> AppendRecoveryTasks(
        IReadOnlyList<AgentTaskGraphEntry> current,
        string failedStage,
        IReadOnlyList<string> reasons);
}
