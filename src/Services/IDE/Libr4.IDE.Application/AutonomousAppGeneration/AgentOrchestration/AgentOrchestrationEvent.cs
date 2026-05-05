namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

public class AgentOrchestrationEvent
{
    public Guid Id { get; init; }
    public Guid RunId { get; init; }
    public AgentInfo RootAgent { get; init; }
    public string? TriggeredBy { get; init; } // "LLM", "user", "system"
    public DateTimeOffset Timestamp { get; init; }

    public AgentOrchestrationEvent(Guid runId, AgentInfo rootAgent, string? triggeredBy = null)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        RootAgent = rootAgent;
        TriggeredBy = triggeredBy;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
