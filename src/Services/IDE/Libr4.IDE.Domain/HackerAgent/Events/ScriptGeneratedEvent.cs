using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.HackerAgent.Events;

/// <summary>
/// Domain event raised when a script is generated
/// </summary>
public class ScriptGeneratedEvent : IDomainEvent
{
    public Guid HackerAgentId { get; }
    public string OperationId { get; }
    public string ScriptName { get; }
    public DateTime OccurredOn { get; }
    
    public ScriptGeneratedEvent(
        Guid hackerAgentId,
        string operationId,
        string scriptName)
    {
        HackerAgentId = hackerAgentId;
        OperationId = operationId;
        ScriptName = scriptName;
        OccurredOn = DateTime.UtcNow;
    }
}
