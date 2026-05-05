namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

public sealed class RuntimePolicyOptions
{
    public int MaxCommandTimeoutSeconds { get; set; } = 600;
    public string[] DenyCommandContains { get; set; } =
    {
        "rm -rf /",
        "mkfs",
        "shutdown",
        "reboot",
        "halt",
        ":(){ :|:& };:",
        "dd if="
    };
}

public interface IRuntimeCommandPolicy
{
    TimeSpan GetCommandTimeout(string command);
    void EnsureCommandAllowed(string command);
}
