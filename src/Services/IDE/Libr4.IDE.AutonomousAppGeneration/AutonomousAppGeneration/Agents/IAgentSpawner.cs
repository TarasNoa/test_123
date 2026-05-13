namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Allows an agent to dynamically spawn subagents for specialized tasks.
/// This enables hierarchical, self-organizing agent teams.
/// </summary>
public interface IAgentSpawner
{
    /// <summary>
    /// Spawn a subagent by its specialized role (e.g., "auth-specialist", "db-migration-expert").
    /// The spawner resolves the role to a SKILL.md and returns a ready-to-use agent.
    /// </summary>
    IAgent SpawnByRole(string role, string? contextHint = null);

    /// <summary>
    /// Spawn a subagent for a specific technology stack and phase.
    /// </summary>
    IAgent SpawnByStack(string stackId, AgentPhase phase);

    /// <summary>
    /// Execute a subagent synchronously and return its result.
    /// Convenience method: spawn + execute in one call.
    /// </summary>
    Task<AgentResult> SpawnAndExecuteAsync(string role, AgentContext context, CancellationToken ct = default);
}
