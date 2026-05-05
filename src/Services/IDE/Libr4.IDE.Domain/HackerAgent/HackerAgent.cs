using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.HackerAgent.Events;

namespace Libr4.IDE.Domain.HackerAgent;

/// <summary>
/// AggregateRoot for hacker agent
/// </summary>
public class HackerAgent : AggregateRoot<Guid>
{
    public string OperationId { get; private set; }
    public string WorkspaceId { get; private set; }
    public List<SecurityScript> Scripts { get; private set; }
    public List<GitHubSecurityTool> Tools { get; private set; }
    public List<string> TestResults { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private HackerAgent() { }
    
    public HackerAgent(
        string operationId,
        string workspaceId,
        List<SecurityScript>? scripts = null,
        List<GitHubSecurityTool>? tools = null)
    {
        Id = Guid.NewGuid();
        OperationId = operationId;
        WorkspaceId = workspaceId;
        Scripts = scripts ?? new List<SecurityScript>();
        Tools = tools ?? new List<GitHubSecurityTool>();
        TestResults = new List<string>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddScript(SecurityScript script)
    {
        if (script != null)
        {
            Scripts.Add(script);
        }
    }
    
    public void AddTool(GitHubSecurityTool tool)
    {
        if (tool != null)
        {
            Tools.Add(tool);
        }
    }
    
    public void AddTestResult(string result)
    {
        if (!string.IsNullOrWhiteSpace(result))
        {
            TestResults.Add(result);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Marks a script as generated and raises a domain event
    /// </summary>
    public void MarkScriptGenerated(SecurityScript script)
    {
        AddDomainEvent(new ScriptGeneratedEvent(Id, OperationId, script.ScriptName));
    }
    
    /// <summary>
    /// Marks a tool as fetched and raises a domain event
    /// </summary>
    public void MarkToolFetched(GitHubSecurityTool tool)
    {
        AddDomainEvent(new ToolFetchedEvent(Id, OperationId, tool.RepoName));
    }
    
    /// <summary>
    /// Marks the security test as completed and raises a domain event
    /// </summary>
    public void MarkSecurityTestCompleted()
    {
        AddDomainEvent(new SecurityTestCompletedEvent(Id, OperationId, Scripts.Count, Tools.Count));
    }
    
    public static HackerAgent Create(
        string operationId,
        string workspaceId,
        List<SecurityScript>? scripts = null,
        List<GitHubSecurityTool>? tools = null)
    {
        return new HackerAgent(operationId, workspaceId, scripts, tools);
    }
}
