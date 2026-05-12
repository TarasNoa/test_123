using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.AI.Application.Agents;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AgentService> _logger;

    public AgentService(IAgentRepository agentRepository, IDistributedCache cache, ILogger<AgentService> logger)
    {
        _agentRepository = agentRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "agents:all";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Retrieved agents from cache");
            return JsonSerializer.Deserialize<List<AgentDto>>(cached)!;
        }

        var agents = await _agentRepository.GetAllAsync(cancellationToken);
        var result = agents.Select(a => new AgentDto(a.Id, a.Name, a.Description, a.Status.ToString().ToLower(), a.CreatedAt)).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return result;
    }

    public async Task<AgentDto?> GetAgentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id, cancellationToken);
        if (agent == null) return null;

        return new AgentDto(
            agent.Id,
            agent.Name,
            agent.Description,
            agent.Status.ToString().ToLower(),
            agent.CreatedAt);
    }

    public async Task<AgentDto> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default)
    {
        var agent = new Agent(
            Guid.NewGuid(),
            request.Name,
            request.Role,
            AgentType.Chat,
            request.Prompt,
            "gpt-4");

        await _agentRepository.AddAsync(agent, cancellationToken);

        return new AgentDto(
            agent.Id,
            agent.Name,
            agent.Description,
            agent.Status.ToString().ToLower(),
            agent.CreatedAt);
    }

    public async Task ActivateAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id, cancellationToken);
        if (agent == null) throw new InvalidOperationException("Agent not found");

        agent.Activate();
        await _agentRepository.UpdateAsync(agent, cancellationToken);
    }

    public async Task DeactivateAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id, cancellationToken);
        if (agent == null) throw new InvalidOperationException("Agent not found");

        agent.Deactivate();
        await _agentRepository.UpdateAsync(agent, cancellationToken);
    }
}