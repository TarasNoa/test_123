using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public abstract class BaseAgent : IAgent
{
    public Guid Id { get; }
    public string Name { get; protected set; }
    public AgentType Type { get; protected set; }
    public Guid? ParentAgentId { get; set; }
    public List<Guid> ChildAgentIds { get; } = new();
    
    protected readonly ILogger<BaseAgent> _logger;
    protected readonly Dictionary<string, IAgent> _childAgents = new();

    protected BaseAgent(ILogger<BaseAgent> logger, string name, AgentType type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        _logger = logger;
    }

    public virtual async Task<AgentResponse> ExecuteAsync(AgentRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation($"Agent {Name} executing task: {request.Task}");

            if (!await CanHandleAsync(request.Task))
            {
                return new AgentResponse
                {
                    RequestId = request.Id,
                    AgentId = Id,
                    Success = false,
                    Error = $"Agent {Name} cannot handle task: {request.Task}",
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            var result = await ExecuteInternalAsync(request);
            stopwatch.Stop();

            return new AgentResponse
            {
                RequestId = request.Id,
                AgentId = Id,
                Success = true,
                Result = result,
                ExecutionTime = stopwatch.Elapsed,
                Confidence = 85
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError($"Error in agent {Name}: {ex.Message}");
            return new AgentResponse
            {
                RequestId = request.Id,
                AgentId = Id,
                Success = false,
                Error = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    protected abstract Task<string> ExecuteInternalAsync(AgentRequest request);

    public abstract Task<bool> CanHandleAsync(string taskType);

    public abstract AgentCapabilities GetCapabilities();

    public async Task RegisterChildAgentAsync(IAgent childAgent)
    {
        if (_childAgents.ContainsKey(childAgent.Id.ToString()))
            return;

        _childAgents[childAgent.Id.ToString()] = childAgent;
        ChildAgentIds.Add(childAgent.Id);
        if (childAgent is BaseAgent baseChild)
            baseChild.ParentAgentId = Id;

        _logger.LogInformation($"Child agent {childAgent.Name} registered to {Name}");
        await Task.CompletedTask;
    }

    public async Task UnregisterChildAgentAsync(Guid childAgentId)
    {
        var key = childAgentId.ToString();
        if (_childAgents.Remove(key))
        {
            ChildAgentIds.Remove(childAgentId);
            _logger.LogInformation($"Child agent {childAgentId} unregistered from {Name}");
        }
        await Task.CompletedTask;
    }

    protected async Task<AgentResponse> DelegateToChildAgentAsync(Guid childAgentId, AgentRequest request)
    {
        var key = childAgentId.ToString();
        if (!_childAgents.TryGetValue(key, out var childAgent))
        {
            return new AgentResponse
            {
                RequestId = request.Id,
                AgentId = Id,
                Success = false,
                Error = $"Child agent {childAgentId} not found"
            };
        }

        _logger.LogInformation($"Delegating task to child agent {childAgent.Name}");
        return await childAgent.ExecuteAsync(request);
    }

    protected async Task<List<AgentResponse>> DelegateToMultipleChildAgentsAsync(
        List<Guid> childAgentIds,
        AgentRequest request)
    {
        var tasks = childAgentIds
            .Where(id => _childAgents.ContainsKey(id.ToString()))
            .Select(id => DelegateToChildAgentAsync(id, request));

        return (await Task.WhenAll(tasks)).ToList();
    }
}