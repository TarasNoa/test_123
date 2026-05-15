using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AgentEvents;

public class AgentSpawnerService : IAgentSpawnerService
{
    private readonly ILogger<AgentSpawnerService> _logger;
    private readonly Dictionary<string, AgentInfo> _agents = new();

    public AgentSpawnerService(ILogger<AgentSpawnerService> logger)
    {
        _logger = logger;
    }

    public Task<string> SpawnAgentAsync(string agentType, string task, string[] targetFiles, string? parentAgentId = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var agent = new AgentInfo
        {
            Id = id,
            Type = agentType,
            Status = "running",
            Task = task,
            TargetFiles = targetFiles,
            ParentAgentId = parentAgentId,
        };
        _agents[id] = agent;
        _logger.LogInformation("Spawned agent {AgentId} of type {AgentType}", id, agentType);
        return Task.FromResult(id);
    }

    public Task<bool> KillAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            agent.Status = "killed";
            _logger.LogInformation("Killed agent {AgentId}", agentId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<AgentInfo?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }
}
