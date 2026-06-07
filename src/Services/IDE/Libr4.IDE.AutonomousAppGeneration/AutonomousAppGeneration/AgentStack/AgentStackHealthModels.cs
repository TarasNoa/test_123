namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public sealed record AgentStackComponentHealth(
    string Name,
    bool Healthy,
    string? Error);

public sealed record AgentStackHealthStatus(
    bool ObscuraHealthy,
    bool ShadowSyncHealthy,
    bool SandboxControllerHealthy,
    bool SecurityScannerHealthy,
    bool QdrantHealthy,
    IReadOnlyList<AgentStackComponentHealth> Components)
{
    public bool AllRequiredHealthy =>
        ObscuraHealthy
        && (!ShadowSyncRequired || ShadowSyncHealthy)
        && (!SandboxRequired || SandboxControllerHealthy)
        && (!ScannerRequired || SecurityScannerHealthy)
        && (!QdrantRequired || QdrantHealthy);

    public bool ShadowSyncRequired { get; init; } = true;

    public bool SandboxRequired { get; init; } = true;

    public bool ScannerRequired { get; init; } = true;

    public bool QdrantRequired { get; init; }
}

public sealed class AgentStackUnhealthyException : InvalidOperationException
{
    public AgentStackUnhealthyException(AgentStackHealthStatus status)
        : base(BuildMessage(status))
    {
        Status = status;
    }

    public AgentStackHealthStatus Status { get; }

    private static string BuildMessage(AgentStackHealthStatus status)
    {
        var unhealthy = status.Components
            .Where(c => !c.Healthy)
            .Select(c => $"{c.Name}:{c.Error ?? "unhealthy"}");
        return $"agent_stack_unhealthy:{string.Join(",", unhealthy)}";
    }
}
