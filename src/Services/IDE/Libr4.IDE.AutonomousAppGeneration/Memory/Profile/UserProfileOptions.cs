namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public sealed class UserProfileOptions
{
    public bool Enabled { get; set; } = true;

    public string UsersRoot { get; set; } = ".libr4/users";

    public int MaxPreferredStacks { get; set; } = 8;

    public int MaxRecurringFailures { get; set; } = 10;

    public int MaxSuccessfulPatterns { get; set; } = 10;

    public int MaxPlanningChars { get; set; } = 2000;
}
