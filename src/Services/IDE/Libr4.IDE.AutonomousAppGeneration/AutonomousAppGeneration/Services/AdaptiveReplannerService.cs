using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Adaptive re-planner that detects repeated failure signatures and generates stage-specific recovery tasks.
/// Prevents looping and duplicate recovery attempts.
/// </summary>
public sealed class AdaptiveReplannerService : IAdaptiveReplannerService
{
    private readonly ILogger<AdaptiveReplannerService> _logger;
    private const int MaxRecoveryAttemptsPerSignature = 3;
    private const int MinOccurrencesForSignature = 2;

    public AdaptiveReplannerService(ILogger<AdaptiveReplannerService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<FailureSignature> DetectFailureSignatures(
        IReadOnlyList<QualityGateResult> gateHistory)
    {
        if (gateHistory.Count == 0)
            return Array.Empty<FailureSignature>();

        var signatures = new Dictionary<string, (List<string> reasons, int count, DateTime latest)>(StringComparer.Ordinal);

        foreach (var gate in gateHistory.Where(g => !g.Passed))
        {
            var key = $"{gate.Stage}:{string.Join("|", gate.Reasons.OrderBy(r => r))}";

            if (signatures.TryGetValue(key, out var existing))
            {
                existing.count++;
                existing.latest = DateTime.UtcNow;
                signatures[key] = existing;
            }
            else
            {
                signatures[key] = (new List<string>(gate.Reasons), 1, DateTime.UtcNow);
            }
        }

        var result = new List<FailureSignature>();
        foreach (var (key, (reasons, count, latest)) in signatures)
        {
            if (count >= MinOccurrencesForSignature)
            {
                var colonIdx = key.IndexOf(':');
                var stage = key.Substring(0, colonIdx);
                var reasonPatterns = key.Substring(colonIdx + 1).Split('|').ToList();

                result.Add(new FailureSignature(
                    stage,
                    reasonPatterns,
                    count,
                    latest));

                _logger.LogInformation(
                    "Detected failure signature: stage={Stage}, patterns={Patterns}, occurrences={Count}",
                    stage, string.Join(",", reasonPatterns), count);
            }
        }

        return result;
    }

    public IReadOnlyList<RecoveryTaskRecommendation> GenerateRecoveryTasks(
        IReadOnlyList<FailureSignature> signatures,
        IReadOnlyList<AgentTaskGraphEntry> currentGraph)
    {
        var recommendations = new List<RecoveryTaskRecommendation>();

        foreach (var signature in signatures)
        {
            var existingRecoveryCount = currentGraph
                .Count(t => t.TaskId.StartsWith("t_recovery_") && t.Notes?.Contains(signature.Stage) == true);

            if (existingRecoveryCount >= MaxRecoveryAttemptsPerSignature)
            {
                _logger.LogWarning(
                    "Stage {Stage} has reached max recovery attempts ({Max}), skipping new recovery task",
                    signature.Stage, MaxRecoveryAttemptsPerSignature);
                continue;
            }

            var task = GenerateStageSpecificRecovery(signature);
            if (!WouldCreateLoop(task, currentGraph))
            {
                recommendations.Add(task);
                _logger.LogInformation(
                    "Generated recovery task for stage {Stage}: {TaskId}",
                    signature.Stage, task.TaskId);
            }
            else
            {
                _logger.LogWarning(
                    "Recovery task for stage {Stage} would create a loop, skipping",
                    signature.Stage);
            }
        }

        return recommendations;
    }

    public bool WouldCreateLoop(
        RecoveryTaskRecommendation task,
        IReadOnlyList<AgentTaskGraphEntry> currentGraph)
    {
        // Check if this recovery task would depend on itself or create a cycle
        var recoveryTasks = currentGraph
            .Where(t => t.TaskId.StartsWith("t_recovery_"))
            .ToList();

        // If there are already recovery tasks for this stage, check for patterns
        var stageRecoveries = recoveryTasks
            .Where(t => t.Notes?.Contains(task.Stage, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (stageRecoveries.Count > 0)
        {
            // Check if the recommended actions are identical to previous recovery attempts
            var lastRecovery = stageRecoveries.Last();
            if (lastRecovery.Notes?.Equals(task.Rationale, StringComparison.Ordinal) == true)
            {
                _logger.LogWarning(
                    "Recovery task for stage {Stage} has identical rationale to previous attempt, would create loop",
                    task.Stage);
                return true;
            }
        }

        return false;
    }

    private RecoveryTaskRecommendation GenerateStageSpecificRecovery(FailureSignature signature)
    {
        var baseId = $"t_recovery_{signature.Stage}_{Guid.NewGuid():N}";
        var taskId = baseId.Length > 50 ? baseId.Substring(0, 50) : baseId;
        var (description, actions, rationale) = GetStageSpecificGuidance(signature);

        return new RecoveryTaskRecommendation(
            taskId,
            signature.Stage,
            description,
            actions,
            rationale);
    }

    private (string description, IReadOnlyList<string> actions, string rationale) GetStageSpecificGuidance(
        FailureSignature signature)
    {
        return signature.Stage.ToLowerInvariant() switch
        {
            "plan" => (
                "Re-plan with stricter constraints and validation",
                new[]
                {
                    "Review plan for circular dependencies",
                    "Validate all required agents are available",
                    "Check runtime image compatibility",
                    "Reduce scope if necessary"
                },
                $"Planning failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            ),

            "generation" => (
                "Re-generate with adjusted model parameters and phase isolation",
                new[]
                {
                    "Reduce max tokens per phase",
                    "Increase temperature for diversity",
                    "Split generation into smaller phases",
                    "Add explicit error handling in generated code"
                },
                $"Generation failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            ),

            "consistency" => (
                "Validate and fix structural consistency issues",
                new[]
                {
                    "Check file references and imports",
                    "Validate type definitions across files",
                    "Ensure all dependencies are declared",
                    "Fix namespace/module conflicts"
                },
                $"Consistency check failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            ),

            "execution" => (
                "Debug and fix execution failures",
                new[]
                {
                    "Review build output for compilation errors",
                    "Check test execution logs",
                    "Validate environment setup",
                    "Fix runtime errors in generated code"
                },
                $"Execution failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            ),

            "fix" => (
                "Apply more aggressive fixes with broader scope",
                new[]
                {
                    "Review all error reports comprehensively",
                    "Increase fix scope beyond immediate errors",
                    "Consider refactoring affected modules",
                    "Add defensive programming patterns"
                },
                $"Fixing failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            ),

            _ => (
                "Generic recovery replan",
                new[]
                {
                    "Review previous execution logs",
                    "Adjust parameters based on failure patterns",
                    "Consider alternative approaches"
                },
                $"Stage {signature.Stage} failed {signature.OccurrenceCount} times with patterns: {string.Join(", ", signature.ReasonPatterns)}"
            )
        };
    }
}
