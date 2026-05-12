using Libr4.IDE.Infrastructure.Sandbox;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Orchestration;

/// <summary>
/// Thin orchestrator that coordinates sandbox execution and event persistence.
/// </summary>
public class AgentOrchestrator
{
    private readonly IAgentEventRepository _repository;
    private readonly ISandboxClient _sandbox;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IAgentEventRepository repository,
        ISandboxClient sandbox,
        ILogger<AgentOrchestrator> logger)
    {
        _repository = repository;
        _sandbox = sandbox;
        _logger = logger;
    }

    public async Task ProcessTaskAsync(Guid agentId, string code, CancellationToken cancellationToken = default)
    {
        var result = await _sandbox.ExecuteAsync(code, "csharp", 30, 512, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Sandbox execution failed for agent {AgentId}: {Error}", agentId, result.Error);
            await _repository.SaveAsync(new AgentEvent(
                AgentEventType.TerminalOutput,
                agentId,
                code,
                result.Error,
                -1,
                (long?)result.DurationMs), cancellationToken);
            return;
        }

        await _repository.SaveAsync(new AgentEvent(
            AgentEventType.TerminalOutput,
            agentId,
            code,
            result.Output,
            0,
            (long?)result.DurationMs), cancellationToken);

        _logger.LogInformation("Agent {AgentId} processed task successfully", agentId);
    }

    public async Task ResetAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        await _repository.SaveAsync(new AgentEvent(
            AgentEventType.TerminalOutput,
            agentId,
            null,
            "Agent reset",
            0,
            0), cancellationToken);

        _logger.LogInformation("Agent {AgentId} reset", agentId);
    }

    public async Task<string> GetAgentStateAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetEventsForRunAsync(agentId, cancellationToken);
        return events.LastOrDefault()?.Type.ToString() ?? "Idle";
    }
}
