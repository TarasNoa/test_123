using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

/// <summary>
/// Stub orchestration tracker for agent calls (in-memory implementation)
/// </summary>
public class AgentOrchestrationTracker : IAgentOrchestrationTracker
{
    private readonly ILogger<AgentOrchestrationTracker> _logger;
    private readonly Dictionary<Guid, AgentOrchestrationEvent> _cache = new();
    private readonly object _lock = new();

    public AgentOrchestrationTracker(ILogger<AgentOrchestrationTracker> logger)
    {
        _logger = logger;
    }

    public Task StartAgentCallAsync(Guid runId, AgentInfo agent, string triggeredBy = "LLM")
    {
        var orchestration = new AgentOrchestrationEvent(runId, agent, triggeredBy);
        
        lock (_lock)
        {
            _cache[runId] = orchestration;
        }

        _logger.LogInformation(
            "[Agent Call Start] RunId: {RunId}, Agent: {AgentName}, TriggeredBy: {TriggeredBy}",
            runId, agent.Name, triggeredBy);
        
        return Task.CompletedTask;
    }

    public Task AddSubAgentAsync(Guid runId, Guid parentAgentId, AgentInfo subAgent)
    {
        _logger.LogInformation(
            "[Sub Agent Add] RunId: {RunId}, Parent: {ParentId}, SubAgent: {SubAgentName}",
            runId, parentAgentId, subAgent.Name);
        return Task.CompletedTask;
    }

    public Task CompleteAgentAsync(Guid runId, Guid agentId, string? output = null)
    {
        _logger.LogInformation("[Agent Complete] RunId: {RunId}, Agent: {AgentId}", runId, agentId);
        return Task.CompletedTask;
    }

    public Task FailAgentAsync(Guid runId, Guid agentId, string error)
    {
        _logger.LogError("[Agent Fail] RunId: {RunId}, Agent: {AgentId}, Error: {Error}", runId, agentId, error);
        return Task.CompletedTask;
    }

    public Task<AgentOrchestrationEvent?> GetOrchestrationAsync(Guid runId)
    {
        lock (_lock)
        {
            _cache.TryGetValue(runId, out var orchestration);
            return Task.FromResult(orchestration);
        }
    }
}
