namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

public class AgentEvent
{
    public Guid Id { get; init; }
    public AgentEventType Type { get; init; }
    public Guid RunId { get; init; }
    public string? Command { get; init; }
    public string? Output { get; init; }
    public int? ExitCode { get; init; }
    public long? DurationMs { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public AgentEvent(
        AgentEventType type,
        Guid runId,
        string? command = null,
        string? output = null,
        int? exitCode = null,
        long? durationMs = null)
    {
        Id = Guid.NewGuid();
        Type = type;
        RunId = runId;
        Command = command;
        Output = output;
        ExitCode = exitCode;
        DurationMs = durationMs;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
