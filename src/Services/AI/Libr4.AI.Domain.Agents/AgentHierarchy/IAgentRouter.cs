using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public interface IAgentRouter
{
    Task<List<Guid>> FindSuitableAgentsAsync(AgentRequest request);
    Task RegisterAgentAsync(IAgent agent);
    Task UnregisterAgentAsync(Guid agentId);
    Task<IAgent?> GetAgentAsync(Guid agentId);
}

public class AgentRouter : IAgentRouter
{
    private readonly Dictionary<Guid, IAgent> _agents = new();
    private readonly ILogger<AgentRouter> _logger;

    public AgentRouter(ILogger<AgentRouter> logger)
    {
        _logger = logger;
    }

    public async Task<List<Guid>> FindSuitableAgentsAsync(AgentRequest request)
    {
        var suitableAgentIds = new List<Guid>();

        foreach (var (agentId, agent) in _agents)
        {
            if (await agent.CanHandleAsync(request.Task))
            {
                suitableAgentIds.Add(agentId);
            }
        }

        // Sort by priority: higher confidence/success rate first
        return suitableAgentIds
            .OrderByDescending(id => _agents[id].GetCapabilities().SuccessRate)
            .ToList();
    }

    public async Task RegisterAgentAsync(IAgent agent)
    {
        if (_agents.ContainsKey(agent.Id))
        {
            _logger.LogWarning($"Agent {agent.Name} already registered");
            return;
        }

        _agents[agent.Id] = agent;
        _logger.LogInformation($"Agent {agent.Name} ({agent.Type}) registered");
        await Task.CompletedTask;
    }

    public async Task UnregisterAgentAsync(Guid agentId)
    {
        if (_agents.Remove(agentId, out var agent))
        {
            _logger.LogInformation($"Agent {agent.Name} unregistered");
        }
        await Task.CompletedTask;
    }

    public async Task<IAgent?> GetAgentAsync(Guid agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return await Task.FromResult(agent);
    }
}