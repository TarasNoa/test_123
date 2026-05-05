namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Machine-readable audit record for one command executed in shadow runtime.
/// </summary>
public sealed class CommandExecutionRecord
{
    public string Phase { get; }
    public string Command { get; }
    public int ExitCode { get; }
    public TimeSpan Duration { get; }
    public string RuntimeProvider { get; }
    public string RuntimeSessionId { get; }
    public DateTime ExecutedAtUtc { get; }

    public CommandExecutionRecord(
        string phase,
        string command,
        int exitCode,
        TimeSpan duration,
        string runtimeProvider,
        string runtimeSessionId,
        DateTime executedAtUtc)
    {
        Phase = string.IsNullOrWhiteSpace(phase) ? "unknown" : phase;
        Command = command ?? string.Empty;
        ExitCode = exitCode;
        Duration = duration;
        RuntimeProvider = string.IsNullOrWhiteSpace(runtimeProvider) ? "unknown" : runtimeProvider;
        RuntimeSessionId = runtimeSessionId ?? string.Empty;
        ExecutedAtUtc = executedAtUtc;
    }
}
