using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Factory that creates and configures a <see cref="SubagentOrchestrator"/>
/// for a specific technology stack and generation phase.
/// </summary>
public sealed class AgentOrchestrationFactory
{
    private readonly AgentSkillRegistry _skillRegistry;
    private readonly IAIService _aiService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentOrchestrationFactory> _logger;

    public AgentOrchestrationFactory(
        AgentSkillRegistry skillRegistry,
        IAIService aiService,
        ILoggerFactory loggerFactory,
        ILogger<AgentOrchestrationFactory> logger)
    {
        _skillRegistry = skillRegistry;
        _aiService = aiService;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a SubagentOrchestrator configured for a specific stack and phase.
    /// </summary>
    public SubagentOrchestrator CreateOrchestrator(string stackId, AgentPhase phase)
    {
        var implementerPath = _skillRegistry.GetSkillPath(stackId, phase);
        var specReviewerPath = _skillRegistry.GetSkillPath("generic", AgentPhase.ReviewSpec);
        var qualityReviewerPath = _skillRegistry.GetSkillPath("generic", AgentPhase.ReviewQuality);

        _logger.LogInformation(
            "Creating orchestrator for stack='{StackId}' phase='{Phase}': implementer={ImplPath}, spec={SpecPath}, quality={QualPath}",
            stackId, phase, implementerPath, specReviewerPath, qualityReviewerPath);

        var implementer = new GenericImplementerAgent(
            implementerPath,
            _aiService,
            _loggerFactory.CreateLogger<GenericImplementerAgent>());

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
            _loggerFactory.CreateLogger<SubagentOrchestrator>());
    }

    /// <summary>
    /// Creates specialized orchestrators for all phases of a full-stack application.
    /// </summary>
    public Dictionary<AgentPhase, SubagentOrchestrator> CreateFullStackOrchestrators(string backendStackId, string? frontendStackId = null)
    {
        var orchestrators = new Dictionary<AgentPhase, SubagentOrchestrator>();

        orchestrators[AgentPhase.Backend] = CreateOrchestrator(backendStackId, AgentPhase.Backend);
        orchestrators[AgentPhase.Database] = CreateOrchestrator("generic", AgentPhase.Database);
        orchestrators[AgentPhase.DevOps] = CreateOrchestrator("generic", AgentPhase.DevOps);
        orchestrators[AgentPhase.Observability] = CreateOrchestrator("generic", AgentPhase.Observability);
        orchestrators[AgentPhase.CICD] = CreateOrchestrator("generic", AgentPhase.CICD);
        orchestrators[AgentPhase.Documentation] = CreateOrchestrator("generic", AgentPhase.Documentation);

        if (!string.IsNullOrWhiteSpace(frontendStackId))
        {
            orchestrators[AgentPhase.Frontend] = CreateOrchestrator(frontendStackId, AgentPhase.Frontend);
        }

        return orchestrators;
    }
}
