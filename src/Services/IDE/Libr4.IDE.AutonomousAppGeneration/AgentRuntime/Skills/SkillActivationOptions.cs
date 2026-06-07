namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

public sealed class SkillActivationOptions
{
    /// <summary>Auto-approve first skill activation per run (no user prompt).</summary>
    public bool AutoApproveFirstActivation { get; set; } = true;

    /// <summary>Max distinct skills activated per agent session.</summary>
    public int MaxActivatedSkillsPerSession { get; set; } = 8;

    /// <summary>Root directory containing Skills/{name}/SKILL.md.</summary>
    public string SkillsRoot { get; set; } = Path.Combine(AppContext.BaseDirectory, "Agents", "Skills");

    /// <summary>Crystallized skills directory (.libr4/skills/crystallized).</summary>
    public string CrystallizedSkillsRoot { get; set; } = ".libr4/skills/crystallized";
}
