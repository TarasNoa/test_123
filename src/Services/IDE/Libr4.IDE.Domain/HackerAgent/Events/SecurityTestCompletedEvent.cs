using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.HackerAgent.Events;

/// <summary>
/// Domain event raised when security test is completed
/// </summary>
public class SecurityTestCompletedEvent : IDomainEvent
{
    public Guid HackerAgentId { get; }
    public string OperationId { get; }
    public int ScriptsCount { get; }
    public int ToolsCount { get; }
    public DateTime OccurredOn { get; }
    
    public SecurityTestCompletedEvent(
        Guid hackerAgentId,
        string operationId,
        int scriptsCount,
        int toolsCount)
    {
        HackerAgentId = hackerAgentId;
        OperationId = operationId;
        ScriptsCount = scriptsCount;
        ToolsCount = toolsCount;
        OccurredOn = DateTime.UtcNow;
    }
}
