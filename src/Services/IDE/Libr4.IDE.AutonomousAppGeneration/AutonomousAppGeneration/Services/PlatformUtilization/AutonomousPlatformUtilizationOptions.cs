namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>
/// When enabled, runs use the full production platform path (all gates, all integrations)
/// and inject a capability briefing so LLM agents know how to use Libr4 tooling.
/// </summary>
public sealed class AutonomousPlatformUtilizationOptions
{
    public const string SectionName = "AutonomousAppGeneration:PlatformUtilization";

    /// <summary>
    /// Use full platform: no benchmark gate skipping, proactive subsystem bootstrap, LLM briefing.
    /// </summary>
    public bool EnableFullPlatformUtilization { get; set; } = true;

    /// <summary>Inject scoped capability guidance (not full catalog) into LLM prompts.</summary>
    public bool InjectCapabilityBriefing { get; set; } = true;

    /// <summary>Scoped (filtered by stack/stage) or Full (legacy dump).</summary>
    public PlatformCapabilityBriefingMode CapabilityBriefingMode { get; set; } = PlatformCapabilityBriefingMode.Scoped;

    /// <summary>Hard cap on briefing size (chars). Scoped mode defaults smaller.</summary>
    public int MaxBriefingChars { get; set; } = 4_500;

    /// <summary>Auto-grant stack SKILL.md consent and pre-select skills for the plan stack.</summary>
    public bool AutoActivateStackSkills { get; set; } = true;

    /// <summary>Enable cascade web/codebase prefetch during planning integration.</summary>
    public bool EnableCascadePrefetch { get; set; } = true;

    /// <summary>Start MCP run host for the run when MCP is configured.</summary>
    public bool WarmMcpRunHost { get; set; } = true;

    /// <summary>Retrieve Hermes / session memory before generation.</summary>
    public bool PrefetchRunMemory { get; set; } = true;

    /// <summary>Orchestrator injects platform playbooks by error signature (not agent tool_search).</summary>
    public bool EnableOrchestratorJitInjection { get; set; } = true;

    /// <summary>Merge learned RepairPlaybook hints into orchestrator JIT blocks.</summary>
    public bool EnableOrchestratorJitLearnedPlaybook { get; set; } = true;
}
