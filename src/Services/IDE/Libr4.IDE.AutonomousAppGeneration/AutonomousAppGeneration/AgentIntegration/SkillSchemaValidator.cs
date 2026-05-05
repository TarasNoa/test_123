namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Strict validator for skill/subagent profiles.
/// Enforces schema contracts and safety constraints on skill definitions.
/// Inspired by QwenLM/qwen-code strict subagent validation.
/// </summary>
public sealed class SkillSchemaValidator
{
    private static readonly string[] ValidSafetyLabels = { "trusted", "review-required", "sandbox-only", "blocked" };
    private static readonly string[] ValidStages = { "planning", "plan", "generation", "generating", "consistency", "fixing", "fix", "review", "post_generation", "post_fix" };

    /// <summary>
    /// Validate a skill definition against schema contracts.
    /// </summary>
    public SkillValidationResult Validate(SkillDefinition skill)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate ID format
        if (string.IsNullOrWhiteSpace(skill.Id))
            errors.Add("Skill ID cannot be empty");
        else if (!skill.Id.Contains('.'))
            warnings.Add("Skill ID should follow reverse-DNS format (e.g., 'libr4.plan.architect')");

        // Validate version format
        if (string.IsNullOrWhiteSpace(skill.Version))
            errors.Add("Skill version cannot be empty");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(skill.Version, @"^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$"))
            errors.Add("Skill version must follow semantic versioning (e.g., '1.0.0')");

        // Validate display name
        if (string.IsNullOrWhiteSpace(skill.DisplayName))
            errors.Add("Skill display name cannot be empty");

        // Validate capability tags
        if (skill.CapabilityTags.Count == 0)
            warnings.Add("Skill has no capability tags");
        else if (skill.CapabilityTags.Any(t => string.IsNullOrWhiteSpace(t)))
            errors.Add("Skill capability tags cannot be empty strings");

        // Validate safety label
        if (string.IsNullOrWhiteSpace(skill.SafetyLabel))
            errors.Add("Skill safety label cannot be empty");
        else if (!ValidSafetyLabels.Contains(skill.SafetyLabel.ToLower()))
            errors.Add($"Invalid safety label '{skill.SafetyLabel}'. Valid values: {string.Join(", ", ValidSafetyLabels)}");

        // Validate applicable stages
        if (skill.ApplicableStages.Count == 0)
            errors.Add("Skill must have at least one applicable stage");
        else
        {
            var invalidStages = skill.ApplicableStages.Where(s => !ValidStages.Contains(s.ToLower())).ToList();
            if (invalidStages.Any())
                errors.Add($"Invalid stages: {string.Join(", ", invalidStages)}. Valid stages: {string.Join(", ", ValidStages)}");
        }

        // Validate model config
        ValidateModelConfig(skill.ModelConfig, errors, warnings);

        // Validate run config
        ValidateRunConfig(skill.RunConfig, errors, warnings);

        // Validate allowed tools
        if (skill.AllowedTools.Count == 0)
            warnings.Add("Skill has no allowed tools - it will be unable to execute");
        else if (skill.AllowedTools.Any(t => string.IsNullOrWhiteSpace(t)))
            errors.Add("Allowed tools cannot contain empty strings");

        // Safety checks for blocked skills
        if (skill.SafetyLabel.Equals("blocked", StringComparison.OrdinalIgnoreCase))
            errors.Add("Skill is marked as 'blocked' and cannot be used");

        return new SkillValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors,
            Warnings: warnings);
    }

    /// <summary>
    /// Validate model configuration constraints.
    /// </summary>
    private void ValidateModelConfig(SkillModelConfig config, List<string> errors, List<string> warnings)
    {
        if (config.Temperature < 0 || config.Temperature > 2)
            errors.Add($"Temperature must be between 0 and 2, got {config.Temperature}");

        if (config.MaxTokens < 1 || config.MaxTokens > 128000)
            errors.Add($"MaxTokens must be between 1 and 128000, got {config.MaxTokens}");

        if (config.UseCascade && string.IsNullOrWhiteSpace(config.ModelHint))
            warnings.Add("Cascade mode is enabled but no ModelHint is specified - will use default cascade model");
    }

    /// <summary>
    /// Validate runtime configuration constraints.
    /// </summary>
    private void ValidateRunConfig(SkillRunConfig config, List<string> errors, List<string> warnings)
    {
        if (config.TimeoutSeconds < 1 || config.TimeoutSeconds > 3600)
            errors.Add($"TimeoutSeconds must be between 1 and 3600, got {config.TimeoutSeconds}");

        if (config.MaxRetries < 0 || config.MaxRetries > 10)
            errors.Add($"MaxRetries must be between 0 and 10, got {config.MaxRetries}");

        if (config.RequiresSandbox && !config.RequiresIsolation)
            warnings.Add("Skill requires sandbox but not isolation - consider enabling isolation for safety");

        if (config.RequiresIsolation && config.TimeoutSeconds < 60)
            warnings.Add("Skill requires isolation but timeout is less than 60s - may not be sufficient");
    }

    /// <summary>
    /// Validate all skills in a registry.
    /// </summary>
    public Dictionary<string, SkillValidationResult> ValidateRegistry(ISkillRegistry registry)
    {
        var results = new Dictionary<string, SkillValidationResult>();
        foreach (var skill in registry.List())
        {
            results[skill.Id] = Validate(skill);
        }
        return results;
    }
}

/// <summary>
/// Result of skill schema validation.
/// </summary>
public sealed record SkillValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool HasWarnings => Warnings.Count > 0;
    public bool HasErrors => Errors.Count > 0;
}
