namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

public enum SkillConsentStatus
{
    Granted,
    Denied,
    Pending
}

public sealed record SkillConsentDecision(SkillConsentStatus Status, string? Reason = null);

public interface ISkillConsentGate
{
    SkillConsentDecision Evaluate(Guid? runId, string skillName, bool autoApprove);

    void RecordGrant(Guid runId, string skillName);

    bool WasGranted(Guid runId, string skillName);
}
