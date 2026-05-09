namespace Libr4.IDE.Infrastructure.Persistence.Entities;

public class AgentOrchestrationEntity
{
    public Guid RunId { get; set; }
    public string JsonData { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
