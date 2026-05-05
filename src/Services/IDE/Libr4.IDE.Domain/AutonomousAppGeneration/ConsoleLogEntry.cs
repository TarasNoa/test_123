namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// A single line of output captured from the shadow workspace execution
/// (stdout or stderr), used by the fixer agent to understand failures.
/// </summary>
public sealed class ConsoleLogEntry
{
    public DateTime Timestamp { get; }
    public string Stream { get; } // "stdout" | "stderr"
    public string Message { get; }

    public ConsoleLogEntry(DateTime timestamp, string stream, string message)
    {
        Timestamp = timestamp;
        Stream = stream ?? "stdout";
        Message = message ?? string.Empty;
    }

    public bool IsError => string.Equals(Stream, "stderr", StringComparison.OrdinalIgnoreCase);
}
