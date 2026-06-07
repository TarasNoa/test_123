namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;

public sealed record DMailMessage(
    string Id,
    Guid RunId,
    string From,
    string To,
    string Payload,
    bool AckRequired,
    DateTime TimestampUtc,
    DateTime? AckedAtUtc = null);

public sealed class DMailOptions
{
    public string RunsRoot { get; set; } = ".logs/runs";
}
