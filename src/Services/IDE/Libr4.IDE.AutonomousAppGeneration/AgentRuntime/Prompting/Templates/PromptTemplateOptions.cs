namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public sealed class PromptTemplateOptions
{
    public bool EnableInstructionTemplates { get; set; } = true;

    public string DefaultVariant { get; set; } = "v1";

    /// <summary>Per-role variant ids for A/B (e.g. repair: [v1, v2]).</summary>
    public Dictionary<string, string[]> AbVariants { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["repair"] = ["v1", "v2"]
    };
}
