using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;
using System.Text.Json;

namespace Libr4.IDE.Infrastructure.Persistence;

public class AgentOrchestrationEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string RootAgentJson { get; set; } = string.Empty;
    public string? TriggeredBy { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public static AgentOrchestrationEntity FromDomain(AgentOrchestrationEvent evt)
    {
        return new AgentOrchestrationEntity
        {
            Id = evt.Id,
            RunId = evt.RunId,
            RootAgentJson = JsonSerializer.Serialize(evt.RootAgent),
            TriggeredBy = evt.TriggeredBy,
            Timestamp = evt.Timestamp,
        };
    }

    public AgentOrchestrationEvent ToDomain()
    {
        var rootAgent = JsonSerializer.Deserialize<AgentInfo>(RootAgentJson) 
            ?? throw new InvalidOperationException("Failed to deserialize RootAgent");
        
        return new AgentOrchestrationEvent(RunId, rootAgent, TriggeredBy)
        {
            Id = Id,
            Timestamp = Timestamp,
        };
    }
}
