namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>How much platform surface area to inject into LLM prompts.</summary>
public enum PlatformCapabilityBriefingMode
{
    /// <summary>Filtered capabilities for stack + stage; JIT via tool_search (recommended).</summary>
    Scoped = 0,

    /// <summary>Legacy full dump — high token cost, tool-obsession risk.</summary>
    Full = 1
}
