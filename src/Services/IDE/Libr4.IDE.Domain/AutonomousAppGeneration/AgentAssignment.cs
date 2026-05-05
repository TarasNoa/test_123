namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Describes which existing IDE agent is responsible for a specific task
/// inside a generation phase. The orchestrator composes assignments from
/// existing agents: CodeReview, SecurityTesting, SemanticBlame, WebSearch,
/// TaskDecomposition, HackerAgent, ArchitecturalGuardrails, etc.
/// </summary>
public sealed class AgentAssignment
{
    /// <summary>Name of an existing IDE agent (e.g. "SecurityTestingAgent").</summary>
    public string AgentName { get; }
    /// <summary>Role played by the agent in this assignment (Planner, Generator, Tester, Fixer, Reviewer).</summary>
    public string Role { get; }
    /// <summary>Concrete task instructions for the agent.</summary>
    public string TaskDescription { get; }

    public AgentAssignment(string agentName, string role, string taskDescription)
    {
        AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));
        Role = role ?? throw new ArgumentNullException(nameof(role));
        TaskDescription = taskDescription ?? string.Empty;
    }
}
