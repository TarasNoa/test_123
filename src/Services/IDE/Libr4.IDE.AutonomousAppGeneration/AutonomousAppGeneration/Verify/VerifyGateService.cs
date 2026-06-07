namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifyGateService : IVerifyGateService
{
    public VerifyGateResult Evaluate(VerifyOrchestrationResult orchestration, VerifyRunPlan plan)
    {
        var reasons = new List<string>();

        if (!orchestration.ShadowPassed)
            reasons.Add("shadow_execution_failed");

        if (!orchestration.ReadinessPassed)
        {
            foreach (var readiness in orchestration.ReadinessResults.Where(r => !r.Ready))
                reasons.Add($"readiness_failed:{readiness.TargetName}:{readiness.Url}");
        }

        if (!orchestration.AgentPassed)
            reasons.Add($"agent_verify_failed:{orchestration.AgentSummary}");

        var passed = reasons.Count == 0;
        var summary = passed
            ? $"verify gate passed ({plan.Recipe.Id})"
            : $"verify gate failed ({plan.Recipe.Id}): {string.Join("; ", reasons)}";

        return new VerifyGateResult(passed, summary, reasons);
    }
}
