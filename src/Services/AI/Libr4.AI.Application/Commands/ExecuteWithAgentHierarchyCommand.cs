using System;
using System.Threading.Tasks;
using Libr4.AI.Domain.Agents.AgentHierarchy;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.Commands;

public class ExecuteWithAgentHierarchyCommand
{
    public Guid WorkspaceId { get; set; }
    public string Task { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ExecuteWithAgentHierarchyCommandHandler : ICommandHandler<ExecuteWithAgentHierarchyCommand, AgentResponse>
{
    private readonly IAgentRouter _router;
    private readonly ILogger<ExecuteWithAgentHierarchyCommandHandler> _logger;

    public ExecuteWithAgentHierarchyCommandHandler(
        IAgentRouter router,
        ILogger<ExecuteWithAgentHierarchyCommandHandler> logger)
    {
        _router = router;
        _logger = logger;
    }

    public async Task<AgentResponse> Handle(ExecuteWithAgentHierarchyCommand command)
    {
        _logger.LogInformation($"Executing task with agent hierarchy: {command.Task}");

        var request = new AgentRequest
        {
            Task = command.Task,
            Context = command.Context,
            Parameters = command.Parameters
        };

        // Find orchestrator
        var agents = await _router.FindSuitableAgentsAsync(request);
        
        // Should include OrchestratorAgent
        var orchestrator = agents.FirstOrDefault();
        if (orchestrator == Guid.Empty)
        {
            throw new InvalidOperationException("Orchestrator agent not found");
        }

        var agent = await _router.GetAgentAsync(orchestrator);
        if (agent == null)
        {
            throw new InvalidOperationException("Orchestrator not available");
        }

        var response = await agent.ExecuteAsync(request);

        _logger.LogInformation($"Agent execution completed with status: {response.Success}");

        return response;
    }
}