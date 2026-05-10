using Libr4.IDE.Domain.FSharp;
using Libr4.IDE.Infrastructure.Sandbox;
using Libr4.IDE.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Orchestration;

/// <summary>
/// Thin orchestrator that coordinates F# domain logic and Rust sandbox execution.
/// This is the "Golden Stack" bridge - no business logic here, just coordination.
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

    /// <summary>
    /// Process a task for an agent using Golden Stack pattern:
    /// 1. Get current state from DB
    /// 2. Validate transition via F#
    /// 3. Execute code in Rust sandbox
    /// 4. Compute new state via F#
    /// 5. Persist to PostgreSQL
    /// </summary>
    public async Task ProcessTaskAsync(Guid agentId, string code, CancellationToken cancellationToken = default)
    {
        // 1. Get current state from DB
        var lastEvent = await _repository.GetLatestByAgentIdAsync(agentId, cancellationToken);
        var currentState = lastEvent?.ToFSharpState() ?? AgentState.Idle;

        // 2. Validate transition via F# (Golden Stack)
        if (!AgentLogic.canAcceptTask(currentState))
        {
            _logger.LogWarning("Agent {AgentId} is busy. Current state: {State}", agentId, AgentLogic.getStateName(currentState));
            return;
        }

        // 3. Execute code in secure Rust sandbox
        var result = await _sandbox.ExecuteAsync(code, "csharp", 30, 512, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Sandbox execution failed for agent {AgentId}: {Error}", agentId, result.Error);
            
            // Transition to error state
            var errorState = AgentLogic.nextState(currentState, AgentEvent.CriticalError(result.Error ?? "Unknown error"));
            await _repository.SaveAsync(new AgentEvent
            {
                Id = Guid.NewGuid(),
                RunId = agentId,
                Type = "Error",
                Timestamp = DateTimeOffset.UtcNow,
                Command = code,
                Output = result.Error,
                ExitCode = -1,
                DurationMs = result.DurationMs
            }, cancellationToken);
            return;
        }

        // 4. Compute new state via F#
        var nextState = AgentLogic.nextState(currentState, AgentEvent.ExecutionCompleted(result.Output ?? ""));

        // 5. Persist event to PostgreSQL
        await _repository.SaveAsync(new AgentEvent
        {
            Id = Guid.NewGuid(),
            RunId = agentId,
            Type = nextState.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Command = code,
            Output = result.Output,
            ExitCode = 0,
            DurationMs = result.DurationMs
        }, cancellationToken);

        _logger.LogInformation("Agent {AgentId} processed task successfully. New state: {State}", agentId, AgentLogic.getStateName(nextState));
    }

    /// <summary>
    /// Reset agent to Idle state
    /// </summary>
    public async Task ResetAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var lastEvent = await _repository.GetLatestByAgentIdAsync(agentId, cancellationToken);
        var currentState = lastEvent?.ToFSharpState() ?? AgentState.Idle;

        var nextState = AgentLogic.nextState(currentState, AgentEvent.Reset);

        await _repository.SaveAsync(new AgentEvent
        {
            Id = Guid.NewGuid(),
            RunId = agentId,
            Type = "Reset",
            Timestamp = DateTimeOffset.UtcNow,
            Command = null,
            Output = "Agent reset to Idle",
            ExitCode = 0,
            DurationMs = 0
        }, cancellationToken);

        _logger.LogInformation("Agent {AgentId} reset to Idle state", agentId);
    }

    /// <summary>
    /// Get current agent state
    /// </summary>
    public async Task<string> GetAgentStateAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var lastEvent = await _repository.GetLatestByAgentIdAsync(agentId, cancellationToken);
        var currentState = lastEvent?.ToFSharpState() ?? AgentState.Idle;
        return AgentLogic.getStateName(currentState);
    }
}
