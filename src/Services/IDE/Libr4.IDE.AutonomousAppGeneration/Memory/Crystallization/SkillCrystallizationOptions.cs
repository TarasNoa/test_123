namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;

public sealed class SkillCrystallizationOptions
{
    public bool Enabled { get; set; } = true;

    public int CrystallizeAfterSuccessCount { get; set; } = 3;

    public string CrystallizedSkillsRoot { get; set; } = ".libr4/skills/crystallized";

    public bool RequireHumanApproval { get; set; } = false;
}
