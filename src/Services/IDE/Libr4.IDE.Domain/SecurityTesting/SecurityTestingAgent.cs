using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.SecurityTesting.Events;

namespace Libr4.IDE.Domain.SecurityTesting;

/// <summary>
/// AggregateRoot for security testing
/// </summary>
public class SecurityTestingAgent : AggregateRoot<Guid>
{
    public string TestId { get; private set; }
    public string WorkspaceId { get; private set; }
    public List<SecurityVulnerability> Vulnerabilities { get; private set; }
    public SecurityTestResult? Result { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private SecurityTestingAgent() { }
    
    public SecurityTestingAgent(
        string testId,
        string workspaceId,
        List<SecurityVulnerability>? vulnerabilities = null,
        SecurityTestResult? result = null)
    {
        Id = Guid.NewGuid();
        TestId = testId;
        WorkspaceId = workspaceId;
        Vulnerabilities = vulnerabilities ?? new List<SecurityVulnerability>();
        Result = result;
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddVulnerability(SecurityVulnerability vulnerability)
    {
        if (vulnerability != null)
        {
            Vulnerabilities.Add(vulnerability);
        }
    }
    
    public void SetResult(SecurityTestResult result)
    {
        Result = result;
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
    /// Marks the security test as started and raises a domain event
    /// </summary>
    public void MarkTestStarted()
    {
        AddDomainEvent(new SecurityTestStartedEvent(Id, TestId));
    }
    
    /// <summary>
    /// Marks a vulnerability as found and raises a domain event
    /// </summary>
    public void MarkVulnerabilityFound(SecurityVulnerability vulnerability)
    {
        AddDomainEvent(new VulnerabilityFoundEvent(Id, TestId, vulnerability.Type.ToString()));
    }
    
    /// <summary>
    /// Marks the security test as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new SecurityTestCompletedEvent(Id, TestId, Vulnerabilities.Count));
    }
    
    public static SecurityTestingAgent Create(
        string testId,
        string workspaceId,
        List<SecurityVulnerability>? vulnerabilities = null,
        SecurityTestResult? result = null)
    {
        return new SecurityTestingAgent(testId, workspaceId, vulnerabilities, result);
    }
}
