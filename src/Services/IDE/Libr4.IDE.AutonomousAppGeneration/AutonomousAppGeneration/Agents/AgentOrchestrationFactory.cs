using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
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
    private readonly IProviderCapabilityMatrix _providerMatrix;
    private readonly IAgentSpawner _spawner;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentOrchestrationFactory> _logger;
    private readonly AgentOrchestrationOptions _options;

    public AgentOrchestrationFactory(
        AgentSkillRegistry skillRegistry,
        IAIService aiService,
        IProviderCapabilityMatrix providerMatrix,
        IAgentSpawner spawner,
        ILoggerFactory loggerFactory,
        ILogger<AgentOrchestrationFactory> logger,
        IOptions<AgentOrchestrationOptions> options)
    {
        _skillRegistry = skillRegistry;
        _aiService = aiService;
        _providerMatrix = providerMatrix;
        _spawner = spawner;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _options = options.Value;
    }

    public SubagentOrchestrator CreateOrchestrator(string stackId, AgentPhase phase) =>
        CreateOrchestrator(stackId, phase, _options);

    public SubagentOrchestrator CreateOrchestrator(string stackId, AgentPhase phase, AgentOrchestrationOptions options)
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
            _spawner,
            _providerMatrix);

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
            maxConcurrency: options.MaxConcurrentTasks,
            spawner: _spawner,
            options: options);
    }

    public Dictionary<AgentPhase, SubagentOrchestrator> CreateForPlan(
        GenerationPlan plan,
        string backendStackId,
        string? frontendStackId = null,
        AgentOrchestrationOptions? optionsOverride = null)
    {
        var options = optionsOverride ?? _options;
        var orchestrators = CreateFullStackOrchestrators(backendStackId, frontendStackId, options);
        return FilterOrchestratorsForPlan(orchestrators, plan, options);
    }

    public Dictionary<AgentPhase, SubagentOrchestrator> CreateFullStackOrchestrators(
        string backendStackId,
        string? frontendStackId = null) =>
        CreateFullStackOrchestrators(backendStackId, frontendStackId, _options);

    public Dictionary<AgentPhase, SubagentOrchestrator> CreateFullStackOrchestrators(
        string backendStackId,
        string? frontendStackId,
        AgentOrchestrationOptions options)
    {
        var orchestrators = new Dictionary<AgentPhase, SubagentOrchestrator>
        {
            [AgentPhase.Backend] = CreateOrchestrator(backendStackId, AgentPhase.Backend, options),
            [AgentPhase.Database] = CreateOrchestrator("generic", AgentPhase.Database, options),
            [AgentPhase.DevOps] = CreateOrchestrator("generic", AgentPhase.DevOps, options),
            [AgentPhase.Observability] = CreateOrchestrator("generic", AgentPhase.Observability, options),
            [AgentPhase.CICD] = CreateOrchestrator("generic", AgentPhase.CICD, options),
            [AgentPhase.Documentation] = CreateOrchestrator("generic", AgentPhase.Documentation, options)
        };

        if (!string.IsNullOrWhiteSpace(frontendStackId))
            orchestrators[AgentPhase.Frontend] = CreateOrchestrator(frontendStackId, AgentPhase.Frontend, options);

        return orchestrators;
    }

    private Dictionary<AgentPhase, SubagentOrchestrator> FilterOrchestratorsForPlan(
        Dictionary<AgentPhase, SubagentOrchestrator> orchestrators,
        GenerationPlan plan) =>
        FilterOrchestratorsForPlan(orchestrators, plan, _options);

    private Dictionary<AgentPhase, SubagentOrchestrator> FilterOrchestratorsForPlan(
        Dictionary<AgentPhase, SubagentOrchestrator> orchestrators,
        GenerationPlan plan,
        AgentOrchestrationOptions options)
    {
        if (options.ExcludeInfrastructurePhases)
        {
            if (!options.UseExpandedJavaReactManifest)
                orchestrators.Remove(AgentPhase.DevOps);

            orchestrators.Remove(AgentPhase.Observability);
            orchestrators.Remove(AgentPhase.CICD);
            orchestrators.Remove(AgentPhase.Documentation);
        }

        if (options.UseFocusedFullStackPhases
            && (StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack
                || StackLayoutHeuristics.UsesBackendFrontendLayout(plan)))
        {
            var keep = new HashSet<AgentPhase>
            {
                AgentPhase.Backend,
                AgentPhase.Frontend,
                AgentPhase.Database
            };

            if (options.UseExpandedJavaReactManifest)
                keep.Add(AgentPhase.DevOps);

            var filtered = orchestrators
                .Where(kv => keep.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (options.UseExpandedJavaReactManifest
                && filtered.TryGetValue(AgentPhase.Database, out _))
            {
                filtered[AgentPhase.Database] = CreateOrchestrator("java", AgentPhase.Backend, options);
            }

            return filtered;
        }

        return orchestrators;
    }
}
