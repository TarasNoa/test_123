namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

public interface IAgentOrchestrationTracker
{
    Task StartAgentCallAsync(Guid runId, AgentInfo agent, string triggeredBy = "LLM");
    Task AddSubAgentAsync(Guid runId, Guid parentAgentId, AgentInfo subAgent);
    Task CompleteAgentAsync(Guid runId, Guid agentId, string? output = null);
    Task FailAgentAsync(Guid runId, Guid agentId, string error);
    Task<AgentOrchestrationEvent?> GetOrchestrationAsync(Guid runId);
}
