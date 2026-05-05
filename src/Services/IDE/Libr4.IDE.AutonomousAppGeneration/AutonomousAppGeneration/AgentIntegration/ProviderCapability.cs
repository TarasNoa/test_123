namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// AI provider capabilities for model routing decisions.
/// </summary>
public sealed record ProviderCapability(
    string ProviderId,
    string ProviderName,
    bool SupportsFunctionCalling,
    bool SupportsStreaming,
    bool SupportsJsonMode,
    bool SupportsSystemPrompts,
    int MaxContextTokens,
    int MaxOutputTokens,
    double CostPer1kTokens);

/// <summary>
/// Stage-specific model requirements for routing decisions.
/// </summary>
public sealed record StageModelRequirement(
    string Stage,
    bool RequiresFunctionCalling,
    bool RequiresStreaming,
    bool RequiresJsonMode,
    int MinContextTokens,
    int MinOutputTokens,
    double MaxCostPer1kTokens);

/// <summary>
/// Model routing decision with provider and model selection.
/// </summary>
public sealed record ModelRoutingDecision(
    string Stage,
    string ProviderId,
    string ModelId,
    string RoutingReason);

/// <summary>
/// Provider capability matrix for stage-level model routing.
/// </summary>
public interface IProviderCapabilityMatrix
{
    /// <summary>
    /// Get all registered providers.
    /// </summary>
    IReadOnlyList<ProviderCapability> GetProviders();

    /// <summary>
    /// Get provider by ID.
    /// </summary>
    ProviderCapability? GetProvider(string providerId);

    /// <summary>
    /// Route a stage to the best provider/model based on requirements.
    /// </summary>
    ModelRoutingDecision RouteStage(string stage, StageModelRequirement requirement);

    /// <summary>
    /// Get stage requirements for a given stage name.
    /// </summary>
    StageModelRequirement? GetStageRequirements(string stage);
}
