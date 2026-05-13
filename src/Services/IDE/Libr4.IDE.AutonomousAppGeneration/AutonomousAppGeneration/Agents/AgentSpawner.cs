using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Default implementation of <see cref="IAgentSpawner"/>.
/// Resolves roles to SKILL.md paths and creates ad-hoc agents on demand.
/// </summary>
public sealed class AgentSpawner : IAgentSpawner
{
    private readonly AgentSkillRegistry _registry;
    private readonly IAIService _aiService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentSpawner> _logger;

    // Dynamic role -> stack mapping. Can be extended at runtime.
    private readonly Dictionary<string, string> _roleToStackMap;

    public AgentSpawner(
        AgentSkillRegistry registry,
        IAIService aiService,
        ILoggerFactory loggerFactory,
        ILogger<AgentSpawner> logger)
    {
        _registry = registry;
        _aiService = aiService;
        _loggerFactory = loggerFactory;
        _logger = logger;

        _roleToStackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Auth & Security
            ["auth-specialist"] = "generic",
            ["jwt-expert"] = "generic",
            ["oauth-specialist"] = "generic",
            ["security-auditor"] = "generic",

            // Data & Database
            ["db-architect"] = "generic",
            ["migration-expert"] = "generic",
            ["query-optimizer"] = "generic",
            ["redis-specialist"] = "generic",
            ["elasticsearch-expert"] = "generic",

            // API & Integration
            ["api-designer"] = "generic",
            ["graphql-specialist"] = "generic",
            ["grpc-expert"] = "generic",
            ["webhook-engineer"] = "generic",
            ["swagger-expert"] = "generic",

            // Frontend specialization
            ["css-expert"] = "generic",
            ["a11y-specialist"] = "generic",
            ["pwa-engineer"] = "generic",
            ["animation-expert"] = "generic",
            ["responsive-designer"] = "generic",

            // DevOps & Infra
            ["k8s-specialist"] = "generic",
            ["terraform-expert"] = "generic",
            ["docker-expert"] = "generic",
            ["monitoring-engineer"] = "generic",
            ["cdn-specialist"] = "generic",

            // Testing & QA
            ["qa-automation"] = "generic",
            ["e2e-specialist"] = "generic",
            ["performance-tester"] = "generic",
            ["penetration-tester"] = "generic",

            // Documentation
            ["tech-writer"] = "generic",
            ["api-documenter"] = "generic",
            ["diagram-expert"] = "generic",

            // Reviewers (always available)
            ["spec-reviewer"] = "generic",
            ["code-reviewer"] = "generic",
            ["architecture-reviewer"] = "generic",
            ["security-reviewer"] = "generic",
        };
    }

    public IAgent SpawnByRole(string role, string? contextHint = null)
    {
        _logger.LogInformation("Spawning subagent for role='{Role}', hint='{Hint}'", role, contextHint);

        var stackId = _roleToStackMap.TryGetValue(role, out var mapped)
            ? mapped
            : _registry.ResolveStackId(contextHint ?? role);

        var skillPath = _registry.GetSkillPath(stackId, AgentPhase.Generic);
        var logger = _loggerFactory.CreateLogger($"Subagent.{role}");

        return new GenericImplementerAgent(skillPath, _aiService, logger);
    }

    public IAgent SpawnByStack(string stackId, AgentPhase phase)
    {
        _logger.LogInformation("Spawning subagent for stack='{StackId}', phase='{Phase}'", stackId, phase);

        var skillPath = _registry.GetSkillPath(stackId, phase);
        var logger = _loggerFactory.CreateLogger($"Subagent.{stackId}.{phase}");

        return new GenericImplementerAgent(skillPath, _aiService, logger);
    }

    public async Task<AgentResult> SpawnAndExecuteAsync(string role, AgentContext context, CancellationToken ct = default)
    {
        var subagent = SpawnByRole(role, context.Description);
        return await subagent.ExecuteAsync(context);
    }

    /// <summary>
    /// Register a custom role mapping at runtime.
    /// </summary>
    public void RegisterRole(string role, string stackId)
    {
        _roleToStackMap[role] = stackId;
        _logger.LogInformation("Registered role '{Role}' -> stack '{StackId}'", role, stackId);
    }
}
