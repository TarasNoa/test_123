namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Captures one retry decision inside an iteration.
/// </summary>
public sealed class RetryEvent
{
    public int Attempt { get; }
    public string Reason { get; }
    public long BackoffMs { get; }
    public DateTime TimestampUtc { get; }

    public RetryEvent(int attempt, string reason, long backoffMs, DateTime timestampUtc)
    {
        Attempt = attempt;
        Reason = reason ?? string.Empty;
        BackoffMs = backoffMs;
        TimestampUtc = timestampUtc;
    }
}
