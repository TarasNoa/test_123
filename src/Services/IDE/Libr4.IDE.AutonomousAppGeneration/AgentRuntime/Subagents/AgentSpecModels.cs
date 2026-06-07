namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed class AgentSpecDocument
{
    public string Name { get; set; } = string.Empty;
    public string? Extend { get; set; }
    public string? Model { get; set; }
    public int? MaxTurns { get; set; }
    public int? MaxTokens { get; set; }
    public List<string> Toolset { get; set; } = new();
    public string Instruction { get; set; } = string.Empty;
    public string? Permissions { get; set; }
    public string? Backend { get; set; }
    public Dictionary<string, string>? BackendConfig { get; set; }
    public AgentSpecBrowserSection? Browser { get; set; }
}

public sealed class AgentSpec
{
    public required string Name { get; init; }
    public string? Model { get; init; }
    public int MaxTurns { get; init; } = 12;
    public int? MaxTokens { get; init; }
    public IReadOnlyList<string> Toolset { get; init; } = Array.Empty<string>();
    public string Instruction { get; init; } = string.Empty;
    public string? Permissions { get; init; }
    public AgentBackendDescriptor Backend { get; init; } = AgentBackendDescriptor.Native;
    public bool IsReadOnly => Permissions?.Contains("read", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class AgentSpecOptions
{
    public string SpecsDirectory { get; set; } = "Agents/Subagents";

    /// <summary>KLIP evolved overrides loaded after bundled specs (same name wins).</summary>
    public string? EvolvedSpecsDirectory { get; set; }

    public List<AgentSpecDocument> SubAgents { get; set; } = new();
}

public static class AgentSpecReservedNames
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "explore",
        "implementer",
        "verify",
        "repair",
        "computer"
    };
}
