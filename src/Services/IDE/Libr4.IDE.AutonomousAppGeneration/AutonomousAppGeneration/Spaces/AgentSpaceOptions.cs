namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class AgentSpaceOptions
{
    public const string SectionName = "AgentSpaces";

    public string StoreDbPath { get; set; } = ".logs/agent-spaces.db";
    public string SpacesRoot { get; set; } = ".logs/spaces";
    public int MaxWorktreesPerSpace { get; set; } = 4;
    public int HardWorktreeCap { get; set; } = 8;
    public int WorktreeRetainHours { get; set; } = 24;
    public string DefaultBaseBranch { get; set; } = "main";
    public string IntegrationBranchName { get; set; } = "space/integration";
    public int MaxParallelLlmPerSpace { get; set; } = 2;
    public int OrchestratorContextReadySeconds { get; set; } = 30;
}
