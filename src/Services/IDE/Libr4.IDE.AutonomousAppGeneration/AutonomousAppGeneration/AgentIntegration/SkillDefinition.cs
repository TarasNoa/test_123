namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Schema-driven skill definition with model configuration, runtime configuration, and tool allowlist.
/// Inspired by QwenLM/qwen-code strict subagent contracts.
/// </summary>
public sealed record SkillDefinition(
    string Id,
    string Version,
    string DisplayName,
    IReadOnlyList<string> CapabilityTags,
    string SafetyLabel,
    IReadOnlyList<string> ApplicableStages,
    SkillModelConfig ModelConfig,
    SkillRunConfig RunConfig,
    IReadOnlyList<string> AllowedTools);

/// <summary>
/// Model configuration for skill execution (model selection, temperature, etc.)
/// </summary>
public sealed record SkillModelConfig(
    string? ModelHint = null,
    double Temperature = 0.7,
    int MaxTokens = 4096,
    bool UseCascade = false);

/// <summary>
/// Runtime configuration for skill execution (timeout, retry policy, etc.)
/// </summary>
public sealed record SkillRunConfig(
    int TimeoutSeconds = 300,
    int MaxRetries = 2,
    bool RequiresSandbox = false,
    bool RequiresIsolation = false);
