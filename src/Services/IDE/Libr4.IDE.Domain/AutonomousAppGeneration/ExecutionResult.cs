namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Outcome of a single shadow-workspace run (build + tests).
/// </summary>
public sealed class ExecutionResult
{
    public bool Succeeded { get; }
    public int ExitCode { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyList<ConsoleLogEntry> Logs { get; }
    public IReadOnlyList<CommandExecutionRecord> CommandExecutions { get; }
    /// <summary>Raw test report (framework specific, e.g. TRX/JUnit contents).</summary>
    public string? TestReport { get; }

    public ExecutionResult(
        bool succeeded,
        int exitCode,
        TimeSpan duration,
        IReadOnlyList<ConsoleLogEntry> logs,
        IReadOnlyList<CommandExecutionRecord>? commandExecutions = null,
        string? testReport = null)
    {
        Succeeded = succeeded;
        ExitCode = exitCode;
        Duration = duration;
        Logs = logs ?? new List<ConsoleLogEntry>();
        CommandExecutions = commandExecutions ?? new List<CommandExecutionRecord>();
        TestReport = testReport;
    }

    public IEnumerable<ConsoleLogEntry> ErrorLogs => Logs.Where(l => l.IsError);
}
