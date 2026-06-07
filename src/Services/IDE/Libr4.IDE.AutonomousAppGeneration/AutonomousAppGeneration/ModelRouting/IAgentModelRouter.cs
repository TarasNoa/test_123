namespace Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

public sealed record AgentModelRouteDecision(
    string Role,
    string PrimaryModel,
    IReadOnlyList<string> FallbackModels,
    AgentModelProfile Profile,
    string RoutingReason)
{
    public IReadOnlyList<string> AllModels =>
        new[] { PrimaryModel }
            .Concat(FallbackModels)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public interface IAgentModelRouter
{
    AgentModelRouteDecision Route(string role, string? yamlModelOverride = null);

    bool IsRoleModelCircuitOpen(string role, string model);

    void RecordRoleModelSuccess(string role, string model);

    void RecordRoleModelFailure(string role, string model);
}
