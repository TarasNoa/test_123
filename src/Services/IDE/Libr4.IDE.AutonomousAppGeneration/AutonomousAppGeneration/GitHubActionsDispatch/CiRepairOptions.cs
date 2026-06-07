namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public sealed class CiRepairOptions
{
    public const string SectionName = "AutonomousAppGeneration:CiRepair";

    public bool AutoSpawnRepairOnCiFail { get; set; } = true;

    public int MaxLogChars { get; set; } = 12_000;

    public int MaxExcerptLines { get; set; } = 120;
}
