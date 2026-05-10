using System;
using System.Threading.Tasks;
using Libr4.AI.Domain.Agents.AgentHierarchy;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure;

public interface IAgentFactory
{
    Task InitializeAgentHierarchyAsync();
}

public class AgentFactory : IAgentFactory
{
    private readonly IAgentRouter _router;
    private readonly OrchestratorAgent _orchestrator;
    private readonly CodeWriterAgent _codeWriter;
    private readonly CodeReviewerAgent _codeReviewer;
    private readonly DebuggerAgent _debugger;
    private readonly ILogger<AgentFactory> _logger;

    public AgentFactory(
        IAgentRouter router,
        OrchestratorAgent orchestrator,
        CodeWriterAgent codeWriter,
        CodeReviewerAgent codeReviewer,
        DebuggerAgent debugger,
        ILogger<AgentFactory> logger)
    {
        _router = router;
        _orchestrator = orchestrator;
        _codeWriter = codeWriter;
        _codeReviewer = codeReviewer;
        _debugger = debugger;
        _logger = logger;
    }

    public async Task InitializeAgentHierarchyAsync()
    {
        _logger.LogInformation("Initializing Agent Hierarchy...");

        // Register all agents
        await _router.RegisterAgentAsync(_codeWriter);
        await _router.RegisterAgentAsync(_codeReviewer);
        await _router.RegisterAgentAsync(_debugger);
        await _router.RegisterAgentAsync(_orchestrator);

        // Set up hierarchy: Orchestrator has all others as children
        await _orchestrator.RegisterChildAgentAsync(_codeWriter);
        await _orchestrator.RegisterChildAgentAsync(_codeReviewer);
        await _orchestrator.RegisterChildAgentAsync(_debugger);

        _logger.LogInformation("Agent Hierarchy initialized successfully!");
    }
}