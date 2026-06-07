using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;

public sealed class BuiltinPromptVarContext
{
    public GenerationPlan? Plan { get; init; }
    public string? WorkspaceHostPath { get; init; }
    public string? BuildLog { get; init; }
    public Guid? RunId { get; init; }
    public BuiltinPromptStage Stage { get; init; } = BuiltinPromptStage.Repairing;
    public int RepairAttempt { get; init; } = 1;
    public IReadOnlyList<string> LastErrors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ManifestFiles { get; init; } = Array.Empty<string>();
    public int WorkspaceListDepth { get; init; } = 2;
    public int BuildLogTailLines { get; init; } = 80;
    public string? JitLibr4Context { get; init; }
    public string? SkillsManifest { get; init; }
    public string? PlatformCapabilities { get; init; }
    public IReadOnlyList<string> ActivatedSkillNames { get; init; } = Array.Empty<string>();
}
