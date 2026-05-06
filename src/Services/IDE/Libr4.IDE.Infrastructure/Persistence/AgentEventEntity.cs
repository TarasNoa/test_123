using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

namespace Libr4.IDE.Infrastructure.Persistence;

public class AgentEventEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid RunId { get; set; }
    public string? Command { get; set; }
    public string? Output { get; set; }
    public int? ExitCode { get; set; }
    public long? DurationMs { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    
    // Optimistic Concurrency: RowVersion for race condition prevention
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public static AgentEventEntity FromDomain(AgentEvent evt)
    {
        return new AgentEventEntity
        {
            Id = evt.Id,
            Type = evt.Type.ToString(),
            RunId = evt.RunId,
            Command = evt.Command,
            Output = evt.Output,
            ExitCode = evt.ExitCode,
            DurationMs = evt.DurationMs,
            Timestamp = evt.Timestamp,
        };
    }

    public AgentEvent ToDomain()
    {
        return new AgentEvent(
            Enum.Parse<AgentEventType>(Type),
            RunId,
            Command,
            Output,
            ExitCode,
            DurationMs)
        {
            Id = Id,
            Timestamp = Timestamp,
        };
    }
}
