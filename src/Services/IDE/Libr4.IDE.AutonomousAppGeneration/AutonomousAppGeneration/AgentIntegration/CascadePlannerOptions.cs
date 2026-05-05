namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class CascadePlannerOptions
{
    public bool EnableLlmAssistedPass { get; set; } = true;
    public bool EnableWebPrefetchContext { get; set; } = false;
    public string PrefetchToolName { get; set; } = "browser.smoke";
    public int MaxPrefetchContextChars { get; set; } = 600;
    // local | api | auto
    public string ModelRoutingProfile { get; set; } = "auto";
    public string? LocalModel { get; set; }
    public string? ApiModel { get; set; }
}
