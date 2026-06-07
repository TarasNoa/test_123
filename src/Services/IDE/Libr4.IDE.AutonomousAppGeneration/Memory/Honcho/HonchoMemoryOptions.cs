namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public sealed class HonchoMemoryOptions
{
    public const string SectionName = "AutonomousAppGeneration:HonchoMemory";

    public bool Enabled { get; set; } = true;

    public string ApiBaseUrl { get; set; } = "https://api.honcho.dev";

    public string ApiKey { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = "libr4";

    public string AgentPeerId { get; set; } = "libr4-agent";

    public bool UseRemoteDialectic { get; set; } = true;

    public bool FallbackToLocalPersona { get; set; } = true;

    public string PersonaRoot { get; set; } = ".libr4/users";

    public string DefaultReasoningLevel { get; set; } = "low";

    public int MaxPlanningChars { get; set; } = 2500;

    public int MaxPersonaConclusions { get; set; } = 24;

    public bool HasRemoteCredentials =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(WorkspaceId);
}
