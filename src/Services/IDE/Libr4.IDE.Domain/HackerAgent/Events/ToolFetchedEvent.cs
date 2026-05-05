using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.HackerAgent.Events;

/// <summary>
/// Domain event raised when a tool is fetched
/// </summary>
public class ToolFetchedEvent : IDomainEvent
{
    public Guid HackerAgentId { get; }
    public string OperationId { get; }
    public string RepoName { get; }
    public DateTime OccurredOn { get; }
    
    public ToolFetchedEvent(
        Guid hackerAgentId,
        string operationId,
        string repoName)
    {
        HackerAgentId = hackerAgentId;
        OperationId = operationId;
        RepoName = repoName;
        OccurredOn = DateTime.UtcNow;
    }
}
