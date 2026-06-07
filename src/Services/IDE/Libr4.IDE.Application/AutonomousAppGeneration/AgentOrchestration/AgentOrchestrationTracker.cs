using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

/// <summary>
/// Orchestration tracker for agent calls (in-memory implementation)
/// </summary>
public class AgentOrchestrationTracker : IAgentOrchestrationTracker
{
    private readonly ILogger<AgentOrchestrationTracker> _logger;
    private readonly IAgentOrchestrationBroadcaster _broadcaster;
    private readonly Dictionary<Guid, AgentOrchestrationEvent> _cache = new();
    private readonly object _lock = new();

    public AgentOrchestrationTracker(
        ILogger<AgentOrchestrationTracker> logger,
        IAgentOrchestrationBroadcaster? broadcaster = null)
    {
        _logger = logger;
        _broadcaster = broadcaster ?? new NullAgentOrchestrationBroadcaster();
    }

    public async Task StartAgentCallAsync(Guid runId, AgentInfo agent, string triggeredBy = "LLM")
    {
        var orchestration = new AgentOrchestrationEvent(runId, agent, triggeredBy);

        lock (_lock)
        {
            _cache[runId] = orchestration;
        }

        _logger.LogInformation(
            "[Agent Call Start] RunId: {RunId}, Agent: {AgentName}, TriggeredBy: {TriggeredBy}",
            runId, agent.Name, triggeredBy);

        await _broadcaster.PublishAsync(new AgentOrchestrationBroadcast("start", runId, orchestration)).ConfigureAwait(false);
    }

    public async Task AddSubAgentAsync(Guid runId, Guid parentAgentId, AgentInfo subAgent)
    {
        AgentOrchestrationEvent? orchestration = null;
        lock (_lock)
        {
            if (_cache.TryGetValue(runId, out var existing))
            {
                existing.RootAgent.AddSubAgent(subAgent);
                orchestration = existing;
            }
        }

        _logger.LogInformation(
            "[Sub Agent Add] RunId: {RunId}, Parent: {ParentId}, SubAgent: {SubAgentName}",
            runId, parentAgentId, subAgent.Name);

        if (orchestration is not null)
        {
            await _broadcaster.PublishAsync(new AgentOrchestrationBroadcast(
                "subagent", runId, orchestration, subAgent.Id)).ConfigureAwait(false);
        }
    }

    public async Task CompleteAgentAsync(Guid runId, Guid agentId, string? output = null)
    {
        _logger.LogInformation("[Agent Complete] RunId: {RunId}, Agent: {AgentId}", runId, agentId);
        AgentOrchestrationEvent? orchestration = null;
        lock (_lock)
        {
            _cache.TryGetValue(runId, out orchestration);
        }

        if (orchestration is not null)
        {
            await _broadcaster.PublishAsync(new AgentOrchestrationBroadcast(
                "complete", runId, orchestration, agentId)).ConfigureAwait(false);
        }
    }

    public async Task FailAgentAsync(Guid runId, Guid agentId, string error)
    {
        _logger.LogError("[Agent Fail] RunId: {RunId}, Agent: {AgentId}, Error: {Error}", runId, agentId, error);
        AgentOrchestrationEvent? orchestration = null;
        lock (_lock)
        {
            _cache.TryGetValue(runId, out orchestration);
        }

        if (orchestration is not null)
        {
            await _broadcaster.PublishAsync(new AgentOrchestrationBroadcast(
                "fail", runId, orchestration, agentId, error)).ConfigureAwait(false);
        }
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
