namespace Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

public static class AgentModelRoleNames
{
    public const string Explore = "explore";
    public const string Implementer = "implementer";
    public const string Verify = "verify";
    public const string Repair = "repair";
    public const string Computer = "computer";

    public static readonly IReadOnlyList<string> All =
    [
        Explore,
        Implementer,
        Verify,
        Repair,
        Computer
    ];

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Implementer;

        var lower = role.Trim().ToLowerInvariant();
        return All.Contains(lower, StringComparer.OrdinalIgnoreCase) ? lower : Implementer;
    }

    public static string FromPipelineStage(string stage)
    {
        var lower = stage.Trim().ToLowerInvariant();
        if (lower.Contains("plan", StringComparison.Ordinal) || lower.Contains("explore", StringComparison.Ordinal))
            return Explore;
        if (lower.Contains("verify", StringComparison.Ordinal))
            return Verify;
        if (lower.Contains("fix", StringComparison.Ordinal) || lower.Contains("repair", StringComparison.Ordinal))
            return Repair;
        if (lower.Contains("computer", StringComparison.Ordinal))
            return Computer;
        if (lower.Contains("generat", StringComparison.Ordinal))
            return Implementer;
        return Repair;
    }
}

public enum AgentModelProfile
{
    Auto,
    OpenRouter,
    Dmr,
    Batch
}

public sealed class AgentModelRoleOptions
{
    public string? Model { get; set; }

    public string? OpenRouterModel { get; set; }

    public string? DmrModel { get; set; }

    public string? BatchModel { get; set; }

    public List<string> FallbackChain { get; set; } = [];
}

public sealed class AgentModelRoutingOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentModels";

    public AgentModelProfile ActiveProfile { get; set; } = AgentModelProfile.Auto;

    public Dictionary<string, AgentModelRoleOptions> Roles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int RoleCircuitFailureThreshold { get; set; } = 3;

    public int RoleCircuitOpenSeconds { get; set; } = 45;
}
