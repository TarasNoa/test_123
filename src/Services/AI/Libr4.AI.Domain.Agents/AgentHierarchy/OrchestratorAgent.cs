using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public class OrchestratorAgent : BaseAgent
{
    private readonly IAgentRouter _router;

    public OrchestratorAgent(ILogger<BaseAgent> logger, IAgentRouter router)
        : base(logger, "OrchestratorAgent", AgentType.Orchestrator)
    {
        _router = router;
    }

    protected override async Task<string> ExecuteInternalAsync(AgentRequest request)
    {
        _logger.LogInformation($"Orchestrator processing: {request.Task}");

        // Decompose complex task into subtasks
        var subtasks = DecomposeTask(request);
        var responses = new List<AgentResponse>();

        foreach (var subtask in subtasks)
        {
            _logger.LogInformation($"Processing subtask: {subtask.Task}");

            // Find appropriate child agent
            var suitableAgents = await _router.FindSuitableAgentsAsync(subtask);

            if (suitableAgents.Any())
            {
                var response = await DelegateToChildAgentAsync(suitableAgents.First(), subtask);
                responses.Add(response);

                if (!response.Success)
                {
                    _logger.LogWarning($"Subtask failed: {response.Error}");
                }
            }
            else
            {
                _logger.LogWarning($"No suitable agent found for subtask: {subtask.Task}");
            }
        }

        // Aggregate results
        return AggregateResults(responses);
    }

    public override Task<bool> CanHandleAsync(string taskType)
    {
        // Orchestrator can handle any task by delegating
        return Task.FromResult(!string.IsNullOrEmpty(taskType));
    }

    public override AgentCapabilities GetCapabilities()
    {
        return new AgentCapabilities
        {
            SupportedTasks = new List<string> { "*" }, // Everything
            SupportedLanguages = new List<string> { "any" },
            MaxConcurrentTasks = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(5),
            SuccessRate = 0.90
        };
    }

    private List<AgentRequest> DecomposeTask(AgentRequest request)
    {
        // Task decomposition logic
        var subtasks = new List<AgentRequest>();

        // Example: If task is "Build and test the project"
        if (request.Task.Contains("build", StringComparison.OrdinalIgnoreCase))
        {
            subtasks.Add(new AgentRequest
            {
                Task = "Analyze project structure",
                Context = request.Context,
                Parameters = request.Parameters
            });

            subtasks.Add(new AgentRequest
            {
                Task = "Generate build script",
                Context = request.Context,
                Parameters = request.Parameters
            });

            subtasks.Add(new AgentRequest
            {
                Task = "Execute build",
                Context = request.Context,
                Parameters = request.Parameters
            });

            subtasks.Add(new AgentRequest
            {
                Task = "Run tests",
                Context = request.Context,
                Parameters = request.Parameters
            });
        }

        return subtasks.Any() ? subtasks : new List<AgentRequest> { request };
    }

    private string AggregateResults(List<AgentResponse> responses)
    {
        var successCount = responses.Count(r => r.Success);
        var totalCount = responses.Count;

        var summary = $"Task completed with {successCount}/{totalCount} successful sub-operations.\n";
        summary += string.Join("\n", responses.Select((r, i) =>
            $"- Step {i + 1}: {(r.Success ? "✅" : "❌")} {r.Result ?? r.Error}"
        ));

        return summary;
    }
}