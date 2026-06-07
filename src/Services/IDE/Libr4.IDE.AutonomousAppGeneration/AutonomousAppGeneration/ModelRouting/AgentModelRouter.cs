using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

public sealed class AgentModelRouter : IAgentModelRouter
{
    private readonly AgentModelRoutingOptions _options;
    private readonly ProviderMatrixOptions _matrixOptions;
    private readonly AutonomousBatchLlmProfileOptions _batchOptions;
    private readonly RoleModelCircuitBreaker _roleCircuit;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentModelRouter> _logger;

    public AgentModelRouter(
        IOptions<AgentModelRoutingOptions> options,
        IOptions<ProviderMatrixOptions> matrixOptions,
        IOptions<AutonomousBatchLlmProfileOptions> batchOptions,
        RoleModelCircuitBreaker roleCircuit,
        IConfiguration configuration,
        ILogger<AgentModelRouter> logger)
    {
        _options = options.Value;
        _matrixOptions = matrixOptions.Value;
        _batchOptions = batchOptions.Value;
        _roleCircuit = roleCircuit;
        _configuration = configuration;
        _logger = logger;
    }

    public AgentModelRouteDecision Route(string role, string? yamlModelOverride = null)
    {
        var normalizedRole = AgentModelRoleNames.Normalize(role);

        var batchOverride = LlmCallPreferenceContext.CurrentPreferences?.ModelOverride;
        if (!string.IsNullOrWhiteSpace(batchOverride))
        {
            return new AgentModelRouteDecision(
                normalizedRole,
                batchOverride,
                GetRoleOptions(normalizedRole).FallbackChain,
                AgentModelProfile.Batch,
                "batch_llm_profile_active");
        }

        if (!string.IsNullOrWhiteSpace(yamlModelOverride))
        {
            return new AgentModelRouteDecision(
                normalizedRole,
                yamlModelOverride.Trim(),
                GetRoleOptions(normalizedRole).FallbackChain,
                AgentModelProfile.Auto,
                "agent_spec_model_override");
        }

        var profile = ResolveActiveProfile();
        var roleOptions = GetRoleOptions(normalizedRole);
        var primary = ResolveModelForProfile(roleOptions, profile);
        var fallbacks = BuildFallbacks(roleOptions, profile, primary);

        _logger.LogDebug(
            "Agent model route role={Role} profile={Profile} model={Model} fallbacks={FallbackCount}",
            normalizedRole,
            profile,
            primary,
            fallbacks.Count);

        return new AgentModelRouteDecision(
            normalizedRole,
            primary,
            fallbacks,
            profile,
            $"agent_models:{profile}:{normalizedRole}");
    }

    public bool IsRoleModelCircuitOpen(string role, string model) =>
        _roleCircuit.IsOpen(role, model);

    public void RecordRoleModelSuccess(string role, string model) =>
        _roleCircuit.OnSuccess(role, model);

    public void RecordRoleModelFailure(string role, string model) =>
        _roleCircuit.OnFailure(role, model);

    private AgentModelProfile ResolveActiveProfile()
    {
        if (_options.ActiveProfile != AgentModelProfile.Auto)
            return _options.ActiveProfile;

        var provider = _configuration["AI:DefaultProvider"] ?? string.Empty;
        if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
            return AgentModelProfile.OpenRouter;

        if (provider.Equals("DockerModelRunner", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            return AgentModelProfile.Dmr;

        return AgentModelProfile.OpenRouter;
    }

    private string ResolveModelForProfile(AgentModelRoleOptions roleOptions, AgentModelProfile profile)
    {
        var selected = profile switch
        {
            AgentModelProfile.OpenRouter => roleOptions.OpenRouterModel ?? roleOptions.Model ?? _matrixOptions.ApiModel,
            AgentModelProfile.Dmr => roleOptions.DmrModel ?? roleOptions.Model ?? _matrixOptions.LocalModel,
            AgentModelProfile.Batch => roleOptions.BatchModel ?? _batchOptions.Model ?? roleOptions.Model,
            _ => roleOptions.Model
        };

        if (!string.IsNullOrWhiteSpace(selected))
            return selected!;

        return profile switch
        {
            AgentModelProfile.Dmr => _matrixOptions.CodeGenerationModel ?? _matrixOptions.FallbackModel,
            _ => _matrixOptions.ApiModel ?? _matrixOptions.FallbackModel
        };
    }

    private static IReadOnlyList<string> BuildFallbacks(
        AgentModelRoleOptions roleOptions,
        AgentModelProfile profile,
        string primary)
    {
        var list = new List<string>();
        list.AddRange(roleOptions.FallbackChain.Where(f => !string.IsNullOrWhiteSpace(f)));

        if (profile == AgentModelProfile.Dmr && !string.IsNullOrWhiteSpace(roleOptions.OpenRouterModel))
            list.Add(roleOptions.OpenRouterModel!);
        if (profile == AgentModelProfile.OpenRouter && !string.IsNullOrWhiteSpace(roleOptions.DmrModel))
            list.Add(roleOptions.DmrModel!);

        return list
            .Where(m => !string.Equals(m, primary, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AgentModelRoleOptions GetRoleOptions(string role)
    {
        if (_options.Roles.TryGetValue(role, out var configured) && configured is not null)
            return configured;

        return CreateDefaultRoleOptions(role);
    }

    private AgentModelRoleOptions CreateDefaultRoleOptions(string role) =>
        role switch
        {
            AgentModelRoleNames.Explore => new AgentModelRoleOptions
            {
                Model = _matrixOptions.ReasoningModel ?? _matrixOptions.FallbackModel,
                OpenRouterModel = "openai/gpt-4o-mini",
                DmrModel = _matrixOptions.ReasoningModel ?? _matrixOptions.LocalModel,
                BatchModel = _batchOptions.Model,
                FallbackChain = [_matrixOptions.FallbackModel]
            },
            AgentModelRoleNames.Verify => new AgentModelRoleOptions
            {
                Model = _matrixOptions.ReasoningModel ?? _matrixOptions.FallbackModel,
                OpenRouterModel = "openai/gpt-4o-mini",
                DmrModel = _matrixOptions.ReasoningModel ?? _matrixOptions.LocalModel,
                BatchModel = _batchOptions.Model,
                FallbackChain = [_matrixOptions.FallbackModel]
            },
            AgentModelRoleNames.Computer => new AgentModelRoleOptions
            {
                Model = _matrixOptions.CodeGenerationModel ?? _matrixOptions.FallbackModel,
                OpenRouterModel = "openai/gpt-4o-mini",
                DmrModel = _matrixOptions.CodeGenerationModel ?? _matrixOptions.LocalModel,
                BatchModel = _batchOptions.Model,
                FallbackChain = [_matrixOptions.FallbackModel]
            },
            AgentModelRoleNames.Repair => new AgentModelRoleOptions
            {
                Model = _matrixOptions.CodeGenerationModel ?? _matrixOptions.FallbackModel,
                OpenRouterModel = _matrixOptions.ApiModel ?? "openai/gpt-4o-mini",
                DmrModel = _matrixOptions.CodeGenerationModel ?? _matrixOptions.LocalModel,
                BatchModel = _batchOptions.Model,
                FallbackChain = [_matrixOptions.FallbackModel]
            },
            _ => new AgentModelRoleOptions
            {
                Model = _matrixOptions.CodeGenerationModel ?? _matrixOptions.FallbackModel,
                OpenRouterModel = _matrixOptions.ApiModel ?? "openai/gpt-4o",
                DmrModel = _matrixOptions.CodeGenerationModel ?? _matrixOptions.LocalModel,
                BatchModel = _batchOptions.Model,
                FallbackChain = [_matrixOptions.FallbackModel]
            }
        };
}
