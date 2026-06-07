using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

public sealed class InMemorySkillConsentGate : ISkillConsentGate
{
    private readonly ConcurrentDictionary<string, byte> _granted = new(StringComparer.OrdinalIgnoreCase);

    public SkillConsentDecision Evaluate(Guid? runId, string skillName, bool autoApprove)
    {
        if (runId is not Guid id)
            return autoApprove
                ? new SkillConsentDecision(SkillConsentStatus.Granted)
                : new SkillConsentDecision(SkillConsentStatus.Pending, "run_id required for skill consent");

        if (WasGranted(id, skillName))
            return new SkillConsentDecision(SkillConsentStatus.Granted);

        if (autoApprove)
        {
            RecordGrant(id, skillName);
            return new SkillConsentDecision(SkillConsentStatus.Granted);
        }

        return new SkillConsentDecision(
            SkillConsentStatus.Pending,
            $"first activation of '{skillName}' requires user consent — retry after approval");
    }

    public void RecordGrant(Guid runId, string skillName) =>
        _granted[BuildKey(runId, skillName)] = 1;

    public bool WasGranted(Guid runId, string skillName) =>
        _granted.ContainsKey(BuildKey(runId, skillName));

    private static string BuildKey(Guid runId, string skillName) =>
        $"{runId:D}:{skillName.Trim().ToLowerInvariant()}";
}
