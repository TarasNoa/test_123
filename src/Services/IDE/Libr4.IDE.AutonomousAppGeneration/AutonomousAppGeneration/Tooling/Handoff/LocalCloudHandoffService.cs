namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Handoff;

public enum HandoffTarget
{
    Local,
    Cloud
}

public sealed record HandoffDecision(HandoffTarget Target, string Reason);

public interface ILocalCloudHandoffService
{
    HandoffDecision Decide(int estimatedDurationMinutes, int contextTokens, bool hasHeavyBuildGraph);
}

public sealed class LocalCloudHandoffService : ILocalCloudHandoffService
{
    public HandoffDecision Decide(int estimatedDurationMinutes, int contextTokens, bool hasHeavyBuildGraph)
    {
        if (estimatedDurationMinutes >= 25 || contextTokens >= 48_000 || hasHeavyBuildGraph)
        {
            return new HandoffDecision(HandoffTarget.Cloud, "Long-running or high-context generation; prefer cloud continuation.");
        }

        return new HandoffDecision(HandoffTarget.Local, "Short task with moderate context; keep local execution.");
    }
}
