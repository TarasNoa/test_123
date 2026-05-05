using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Runtime enforcement of skill safety-label operational policies.
/// Translates metadata safety labels into executable allow/deny decisions.
/// </summary>
public sealed class SkillSafetyPolicyService : ISkillSafetyPolicyService
{
    private readonly ILogger<SkillSafetyPolicyService> _logger;

    public SkillSafetyPolicyService(ILogger<SkillSafetyPolicyService> logger)
    {
        _logger = logger;
    }

    public SkillSafetyDecision EvaluateSkillExecution(
        SkillDefinition skill,
        string stage,
        string lane)
    {
        var label = skill.SafetyLabel.ToLowerInvariant();

        return label switch
        {
            "trusted" => new SkillSafetyDecision(
                skill.Id,
                true,
                "Skill is marked as trusted",
                Array.Empty<string>()),

            "review-required" => new SkillSafetyDecision(
                skill.Id,
                true,
                "Skill requires review but can execute",
                new[] { "human_review_completed", "execution_logged" }),

            "sandbox-only" => new SkillSafetyDecision(
                skill.Id,
                true,
                "Skill can execute only in sandbox environment",
                new[] { "sandbox_enabled", "isolation_enforced" }),

            "blocked" => new SkillSafetyDecision(
                skill.Id,
                false,
                "Skill is blocked and cannot be executed",
                Array.Empty<string>()),

            _ => new SkillSafetyDecision(
                skill.Id,
                false,
                $"Unknown safety label: {skill.SafetyLabel}",
                Array.Empty<string>())
        };
    }

    public bool RequiresReview(SkillDefinition skill)
    {
        var label = skill.SafetyLabel.ToLowerInvariant();
        var requires = label == "review-required";

        _logger.LogDebug(
            "Skill {SkillId} requires review: {Requires}",
            skill.Id, requires);

        return requires;
    }

    public bool RequiresSandbox(SkillDefinition skill)
    {
        var label = skill.SafetyLabel.ToLowerInvariant();
        var requires = label == "sandbox-only" || skill.RunConfig.RequiresSandbox;

        _logger.LogDebug(
            "Skill {SkillId} requires sandbox: {Requires}",
            skill.Id, requires);

        return requires;
    }

    public IReadOnlyList<string> GetBlockedSkills(IReadOnlyList<SkillDefinition> skills)
    {
        var blocked = skills
            .Where(s => s.SafetyLabel.Equals("blocked", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .ToList();

        _logger.LogInformation(
            "Found {Count} blocked skills",
            blocked.Count);

        return blocked;
    }

    public bool ValidateExecutionCompliance(
        SkillDefinition skill,
        string stage,
        string lane,
        IReadOnlyList<string> fulfilledConditions)
    {
        var decision = EvaluateSkillExecution(skill, stage, lane);

        // If skill is not allowed, execution is not compliant
        if (!decision.IsAllowed)
        {
            _logger.LogWarning(
                "Skill {SkillId} execution not allowed: {Reason}",
                skill.Id, decision.Reason);
            return false;
        }

        // Check if all required conditions are fulfilled
        var missingConditions = decision.RequiredConditions
            .Where(c => !fulfilledConditions.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingConditions.Count > 0)
        {
            _logger.LogWarning(
                "Skill {SkillId} execution missing conditions: {Conditions}",
                skill.Id, string.Join(", ", missingConditions));
            return false;
        }

        _logger.LogInformation(
            "Skill {SkillId} execution is compliant with safety policy",
            skill.Id);

        return true;
    }
}
