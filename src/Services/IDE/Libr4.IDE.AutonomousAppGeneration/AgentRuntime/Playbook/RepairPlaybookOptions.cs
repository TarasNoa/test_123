namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public sealed class RepairPlaybookOptions
{
    public string DbPath { get; set; } = ".logs/agent-runtime/repair-playbook.db";

    public int MinAttemptsBeforeHint { get; set; } = 2;

    public double MinScoreForHint { get; set; } = 0.5;

    public double FailScoreDecay { get; set; } = 0.85;
}
