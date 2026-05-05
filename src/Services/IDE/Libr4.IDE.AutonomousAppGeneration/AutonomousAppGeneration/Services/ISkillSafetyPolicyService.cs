using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Safety policy decision for skill execution.
/// </summary>
public sealed record SkillSafetyDecision(
    string SkillId,
    bool IsAllowed,
    string Reason,
    IReadOnlyList<string> RequiredConditions);

/// <summary>
/// Service for enforcing skill safety-label operational policies at runtime.
/// Translates safety labels (trusted, review-required, sandbox-only, blocked) into allow/deny decisions.
/// </summary>
public interface ISkillSafetyPolicyService
{
    /// <summary>
    /// Evaluate if a skill can be executed in the given stage and lane.
    /// </summary>
    SkillSafetyDecision EvaluateSkillExecution(
        SkillDefinition skill,
        string stage,
        string lane);

    /// <summary>
    /// Check if a skill requires human review before execution.
    /// </summary>
    bool RequiresReview(SkillDefinition skill);

    /// <summary>
    /// Check if a skill requires sandbox isolation.
    /// </summary>
    bool RequiresSandbox(SkillDefinition skill);

    /// <summary>
    /// Get all skills that are blocked in the current policy.
    /// </summary>
    IReadOnlyList<string> GetBlockedSkills(IReadOnlyList<SkillDefinition> skills);

    /// <summary>
    /// Validate that a skill execution request complies with safety policy.
    /// </summary>
    bool ValidateExecutionCompliance(
        SkillDefinition skill,
        string stage,
        string lane,
        IReadOnlyList<string> fulfilledConditions);
}
