using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Output of a single command executed inside an <see cref="IRuntimeSession"/>.
/// </summary>
public sealed class ExecResult
{
    public int ExitCode { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyList<ConsoleLogEntry> Logs { get; }

    public ExecResult(int exitCode, TimeSpan duration, IReadOnlyList<ConsoleLogEntry> logs)
    {
        ExitCode = exitCode;
        Duration = duration;
        Logs = logs ?? new List<ConsoleLogEntry>();
    }

    public bool Succeeded => ExitCode == 0;
}
