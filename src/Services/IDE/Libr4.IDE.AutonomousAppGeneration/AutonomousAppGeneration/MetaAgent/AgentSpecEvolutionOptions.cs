namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public sealed class AgentSpecEvolutionOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentSpecEvolution";

    public bool Enabled { get; set; } = true;

    public bool AutoAnalyzeFailedRuns { get; set; } = true;

    public string ProposalsDbPath { get; set; } = ".libr4/agent-spec-evolution.db";

    public string VersionsRoot { get; set; } = ".libr4/agent-specs/versions";

    public string EvolvedSpecsRoot { get; set; } = ".libr4/agent-specs/evolved";

    public string BundledSpecsDirectory { get; set; } = "Agents/Subagents";
}
