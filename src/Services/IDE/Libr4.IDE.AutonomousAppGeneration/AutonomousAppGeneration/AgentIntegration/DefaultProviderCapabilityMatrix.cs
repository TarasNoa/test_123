using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Default provider capability matrix with stage-level model routing.
/// </summary>
public sealed class DefaultProviderCapabilityMatrix : IProviderCapabilityMatrix
{
    private readonly ILogger<DefaultProviderCapabilityMatrix> _logger;
    private readonly ProviderMatrixOptions _options;
    private readonly List<ProviderCapability> _providers;
    private readonly Dictionary<string, StageModelRequirement> _stageRequirements;

    public DefaultProviderCapabilityMatrix(
        ILogger<DefaultProviderCapabilityMatrix> logger,
        IOptions<ProviderMatrixOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _providers = InitializeProviders();
        _stageRequirements = InitializeStageRequirements();
    }

    public IReadOnlyList<ProviderCapability> GetProviders() => _providers.AsReadOnly();

    public ProviderCapability? GetProvider(string providerId) =>
        _providers.FirstOrDefault(p => p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    public ModelRoutingDecision RouteStage(string stage, StageModelRequirement requirement)
    {
        var normalizedStage = stage.Trim().ToLowerInvariant();

        if (_options.StageOverrides.TryGetValue(normalizedStage, out var stageOverride)
            && !string.IsNullOrWhiteSpace(stageOverride.ProviderId))
        {
            var overrideProvider = GetProvider(stageOverride.ProviderId)
                                   ?? throw new InvalidOperationException(
                                       $"Stage override provider '{stageOverride.ProviderId}' is not registered.");
            var overrideModel = !string.IsNullOrWhiteSpace(stageOverride.ModelId)
                ? stageOverride.ModelId!
                : SelectModelForStage(overrideProvider, normalizedStage);
            return new ModelRoutingDecision(
                stage,
                overrideProvider.ProviderId,
                overrideModel,
                $"stage_override:{normalizedStage}");
        }

        // Check if we have explicit stage requirements
        var stageReq = _stageRequirements.GetValueOrDefault(normalizedStage, requirement);
        
        // Filter providers that meet requirements
        var eligibleProviders = _providers
            .Where(p =>
                (!stageReq.RequiresFunctionCalling || p.SupportsFunctionCalling) &&
                (!stageReq.RequiresStreaming || p.SupportsStreaming) &&
                (!stageReq.RequiresJsonMode || p.SupportsJsonMode) &&
                p.MaxContextTokens >= stageReq.MinContextTokens &&
                p.MaxOutputTokens >= stageReq.MinOutputTokens &&
                p.CostPer1kTokens <= stageReq.MaxCostPer1kTokens)
            .ToList();

        if (eligibleProviders.Count == 0)
        {
            throw new InvalidOperationException(
                $"No eligible LLM provider for stage '{stage}' (requirements not satisfied by configured providers).");
        }

        ProviderCapability? selected = null;
        if (!string.IsNullOrWhiteSpace(_options.DefaultProvider))
        {
            selected = eligibleProviders.FirstOrDefault(p =>
                p.ProviderId.Equals(_options.DefaultProvider, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                throw new InvalidOperationException(
                    $"Configured provider '{_options.DefaultProvider}' is not eligible for stage '{stage}'.");
            }
        }
        else
        {
            selected = eligibleProviders
                .OrderBy(p => p.CostPer1kTokens)
                .ThenByDescending(p => p.MaxContextTokens)
                .First();
        }

        var modelId = SelectModelForStage(selected, normalizedStage);
        var routingReason = BuildRoutingReason(selected, normalizedStage, modelId);

        return new ModelRoutingDecision(
            Stage: stage,
            ProviderId: selected.ProviderId,
            ModelId: modelId,
            RoutingReason: routingReason);
    }

    private static string BuildRoutingReason(ProviderCapability provider, string stage, string modelId)
    {
        var profile = IsCodeGenerationStage(stage) ? "code_generation_specialist"
            : IsReasoningStage(stage) ? "high_level_reasoning"
            : "default";
        return $"stage_model_routing:{profile}:{provider.ProviderName}";
    }

    public StageModelRequirement? GetStageRequirements(string stage)
    {
        var normalized = stage.Trim().ToLowerInvariant();
        return _stageRequirements.GetValueOrDefault(normalized);
    }

    private string SelectModelForStage(ProviderCapability provider, string stage)
    {
        return provider.ProviderId switch
        {
            "openrouter" => SelectOpenRouterModel(stage),
            "alibabacloud" => SelectApiModel(stage),
            "openai" => SelectOpenAIModel(stage),
            "anthropic" => SelectAnthropicModel(stage),
            "dockermodelrunner" or "ollama" => SelectLocalModel(stage),
            _ => _options.FallbackModel
        };
    }

    private string SelectLocalModel(string stage)
    {
        const string defaultReasoning =
            "huggingface.co/hesamation/qwen3.6-35b-a3b-claude-4.6-opus-reasoning-distilled-gguf:Q4_K_M";
        const string defaultCoder = "huggingface.co/jackrong/qwopus3.5-9b-coder-gguf:Q4_K_M";

        if (IsCodeGenerationStage(stage))
        {
            return _options.CodeGenerationModel
                   ?? _options.LocalModel
                   ?? defaultCoder;
        }

        if (IsReasoningStage(stage))
        {
            return _options.ReasoningModel
                   ?? _options.LocalModel
                   ?? defaultReasoning;
        }

        return _options.LocalModel ?? defaultReasoning;
    }

    private static bool IsCodeGenerationStage(string stage) =>
        stage.Contains("generation", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("fixing", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("consistency", StringComparison.OrdinalIgnoreCase);

    private static bool IsReasoningStage(string stage) =>
        stage.Contains("plan", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("review", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("security", StringComparison.OrdinalIgnoreCase);

    private string SelectApiModel(string stage)
    {
        return !string.IsNullOrWhiteSpace(_options.ApiModel)
            ? _options.ApiModel
            : _options.FallbackModel;
    }

    private string SelectOpenRouterModel(string stage) => SelectApiModel(stage);

    private string SelectOpenAIModel(string stage)
    {
        return stage switch
        {
            var s when s.Contains("plan") => "gpt-4o",
            var s when s.Contains("generation") => "gpt-4o",
            var s when s.Contains("fix") => "gpt-4o",
            var s when s.Contains("consistency") => "gpt-4o-mini",
            var s when s.Contains("review") => "gpt-4o-mini",
            _ => "gpt-4o-mini"
        };
    }

    private string SelectAnthropicModel(string stage)
    {
        return stage switch
        {
            var s when s.Contains("plan") => "claude-3-5-sonnet-20241022",
            var s when s.Contains("generation") => "claude-3-5-sonnet-20241022",
            var s when s.Contains("fix") => "claude-3-5-sonnet-20241022",
            var s when s.Contains("consistency") => "claude-3-5-haiku-20241022",
            var s when s.Contains("review") => "claude-3-5-haiku-20241022",
            _ => "claude-3-5-haiku-20241022"
        };
    }

    private static List<ProviderCapability> InitializeProviders()
    {
        return new List<ProviderCapability>
        {
            // Local providers — zero cost, always prefer first if available.
            new(
                ProviderId: "dockermodelrunner",
                ProviderName: "DockerModelRunner",
                SupportsFunctionCalling: false,
                SupportsStreaming: true,
                SupportsJsonMode: false,
                SupportsSystemPrompts: true,
                MaxContextTokens: 32768,
                MaxOutputTokens: 16000,
                CostPer1kTokens: 0.0),
            new(
                ProviderId: "ollama",
                ProviderName: "Ollama",
                SupportsFunctionCalling: false,
                SupportsStreaming: true,
                SupportsJsonMode: false,
                SupportsSystemPrompts: true,
                MaxContextTokens: 32768,
                MaxOutputTokens: 8192,
                CostPer1kTokens: 0.0),
            new(
                ProviderId: "alibabacloud",
                ProviderName: "AlibabaCloud",
                SupportsFunctionCalling: true,
                SupportsStreaming: true,
                SupportsJsonMode: true,
                SupportsSystemPrompts: true,
                MaxContextTokens: 128000,
                MaxOutputTokens: 16000,
                CostPer1kTokens: 0.0008),
            new(
                ProviderId: "openrouter",
                ProviderName: "OpenRouter",
                SupportsFunctionCalling: true,
                SupportsStreaming: true,
                SupportsJsonMode: true,
                SupportsSystemPrompts: true,
                MaxContextTokens: 200000,
                MaxOutputTokens: 8192,
                CostPer1kTokens: 0.001),
            
            new(
                ProviderId: "openai",
                ProviderName: "OpenAI",
                SupportsFunctionCalling: true,
                SupportsStreaming: true,
                SupportsJsonMode: true,
                SupportsSystemPrompts: true,
                MaxContextTokens: 128000,
                MaxOutputTokens: 4096,
                CostPer1kTokens: 0.005),
            
            new(
                ProviderId: "anthropic",
                ProviderName: "Anthropic",
                SupportsFunctionCalling: true,
                SupportsStreaming: true,
                SupportsJsonMode: false,
                SupportsSystemPrompts: true,
                MaxContextTokens: 200000,
                MaxOutputTokens: 8192,
                CostPer1kTokens: 0.003)
        };
    }

    private static Dictionary<string, StageModelRequirement> InitializeStageRequirements()
    {
        // RequiresJsonMode: false — local models (DockerModelRunner/Ollama) handle JSON via output
        // parsing, not a formal "JSON mode" API flag. Setting to false ensures local providers
        // remain eligible and are preferred (CostPer1kTokens = 0.0).
        return new Dictionary<string, StageModelRequirement>(StringComparer.OrdinalIgnoreCase)
        {
            ["planning"] = new(
                Stage: "planning",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: false,
                MinContextTokens: 8000,
                MinOutputTokens: 2048,
                MaxCostPer1kTokens: 0.01),
            
            ["generation"] = new(
                Stage: "generation",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: false,
                MinContextTokens: 8000,
                MinOutputTokens: 8192,
                MaxCostPer1kTokens: 0.01),
            
            ["fixing"] = new(
                Stage: "fixing",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: false,
                MinContextTokens: 8000,
                MinOutputTokens: 8192,
                MaxCostPer1kTokens: 0.01),
            
            ["consistency"] = new(
                Stage: "consistency",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: false,
                MinContextTokens: 8000,
                MinOutputTokens: 2048,
                MaxCostPer1kTokens: 0.005),
            
            ["review"] = new(
                Stage: "review",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: false,
                MinContextTokens: 16000,
                MinOutputTokens: 2048,
                MaxCostPer1kTokens: 0.005)
        };
    }
}

/// <summary>
/// Configuration options for provider capability matrix.
/// </summary>
public sealed class ProviderMatrixOptions
{
    public const string SectionName = "ProviderCapabilityMatrix";
    
    public string FallbackModel { get; set; } = "openai/gpt-4o-mini";
    public string DefaultProvider { get; set; } = "openrouter";

    /// <summary>
    /// Model ID to use when routing to local providers (DockerModelRunner / Ollama).
    /// Overrides the hard-coded default in SelectModelForStage.
    /// </summary>
    public string? LocalModel { get; set; }

    /// <summary>
    /// Model ID to use when routing to API providers (OpenRouter etc.).
    /// When set, overrides stage-specific model selection.
    /// </summary>
    public string? ApiModel { get; set; }

    /// <summary>
    /// 35B (or similar) model for planning, security review, and other reasoning-heavy stages.
    /// Falls back to <see cref="LocalModel"/>.
    /// </summary>
    public string? ReasoningModel { get; set; }

    /// <summary>
    /// Fast coder model for generation, fixing, database, frontend, and tests.
    /// Falls back to <see cref="LocalModel"/>.
    /// </summary>
    public string? CodeGenerationModel { get; set; }

    /// <summary>Per-stage provider/model override (stage name -> override).</summary>
    public Dictionary<string, StageProviderOverride> StageOverrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class StageProviderOverride
{
    public string? ProviderId { get; set; }

    public string? ModelId { get; set; }
}
