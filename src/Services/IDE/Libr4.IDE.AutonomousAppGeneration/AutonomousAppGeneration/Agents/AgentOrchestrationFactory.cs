using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Factory that creates <see cref="SubagentOrchestrator"/> instances per stack/phase.
/// </summary>
public sealed class AgentOrchestrationFactory
{
    private readonly AgentSkillRegistry _skillRegistry;
    private readonly IAIService _aiService;
    private readonly IAgentSpawner _spawner;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentOrchestrationFactory> _logger;
    private readonly AgentOrchestrationOptions _options;

    public AgentOrchestrationFactory(
        AgentSkillRegistry skillRegistry,
        IAIService aiService,
        IAgentSpawner spawner,
        ILoggerFactory loggerFactory,
        ILogger<AgentOrchestrationFactory> logger,
        IOptions<AgentOrchestrationOptions> options)
    {
        _skillRegistry = skillRegistry;
        _aiService = aiService;
        _spawner = spawner;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _options = options.Value;
    }

    public SubagentOrchestrator CreateOrchestrator(string stackId, AgentPhase phase)
    {
        var implementerPath = _skillRegistry.GetSkillPath(stackId, phase);
        var specReviewerPath = _skillRegistry.GetSkillPath("generic", AgentPhase.ReviewSpec);
        var qualityReviewerPath = _skillRegistry.GetSkillPath("generic", AgentPhase.ReviewQuality);

        _logger.LogInformation(
            "Creating orchestrator for stack='{StackId}' phase='{Phase}'",
            stackId, phase);

        var implementer = new GenericImplementerAgent(
            implementerPath,
            _aiService,
            _loggerFactory.CreateLogger<GenericImplementerAgent>(),
            _spawner);

        var specReviewer = new SpecReviewerAgent(
            specReviewerPath,
            _aiService,
            _loggerFactory.CreateLogger<SpecReviewerAgent>());

        var qualityReviewer = new CodeQualityReviewerAgent(
            qualityReviewerPath,
            _aiService,
            _loggerFactory.CreateLogger<CodeQualityReviewerAgent>());

        return new SubagentOrchestrator(
            implementer,
            specReviewer,
            qualityReviewer,
            _loggerFactory.CreateLogger<SubagentOrchestrator>(),
            maxConcurrency: _options.MaxConcurrentTasks,
            spawner: _spawner,
            options: _options);
    }

    public Dictionary<AgentPhase, SubagentOrchestrator> CreateForPlan(
        GenerationPlan plan,
        string backendStackId,
        string? frontendStackId = null)
    {
        var orchestrators = CreateFullStackOrchestrators(backendStackId, frontendStackId);
        return FilterOrchestratorsForPlan(orchestrators, plan);
    }

    public Dictionary<AgentPhase, SubagentOrchestrator> CreateFullStackOrchestrators(
        string backendStackId,
        string? frontendStackId = null)
    {
        var orchestrators = new Dictionary<AgentPhase, SubagentOrchestrator>
        {
            [AgentPhase.Backend] = CreateOrchestrator(backendStackId, AgentPhase.Backend),
            [AgentPhase.Database] = CreateOrchestrator("generic", AgentPhase.Database),
            [AgentPhase.DevOps] = CreateOrchestrator("generic", AgentPhase.DevOps),
            [AgentPhase.Observability] = CreateOrchestrator("generic", AgentPhase.Observability),
            [AgentPhase.CICD] = CreateOrchestrator("generic", AgentPhase.CICD),
            [AgentPhase.Documentation] = CreateOrchestrator("generic", AgentPhase.Documentation)
        };

        if (!string.IsNullOrWhiteSpace(frontendStackId))
            orchestrators[AgentPhase.Frontend] = CreateOrchestrator(frontendStackId, AgentPhase.Frontend);

        return orchestrators;
    }

    private Dictionary<AgentPhase, SubagentOrchestrator> FilterOrchestratorsForPlan(
        Dictionary<AgentPhase, SubagentOrchestrator> orchestrators,
        GenerationPlan plan)
    {
        if (_options.ExcludeInfrastructurePhases)
        {
            orchestrators.Remove(AgentPhase.DevOps);
            orchestrators.Remove(AgentPhase.Observability);
            orchestrators.Remove(AgentPhase.CICD);
            orchestrators.Remove(AgentPhase.Documentation);
        }

        if (_options.UseFocusedFullStackPhases
            && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack)
        {
            var keep = new HashSet<AgentPhase>
            {
                AgentPhase.Backend,
                AgentPhase.Frontend,
                AgentPhase.Database
            };
            return orchestrators
                .Where(kv => keep.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        return orchestrators;
    }
}
