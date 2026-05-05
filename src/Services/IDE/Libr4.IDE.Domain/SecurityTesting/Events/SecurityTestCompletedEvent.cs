using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SecurityTesting.Events;

/// <summary>
/// Domain event raised when security test is completed
/// </summary>
public class SecurityTestCompletedEvent : IDomainEvent
{
    public Guid SecurityTestingAgentId { get; }
    public string TestId { get; }
    public int VulnerabilitiesCount { get; }
    public DateTime OccurredOn { get; }
    
    public SecurityTestCompletedEvent(
        Guid securityTestingAgentId,
        string testId,
        int vulnerabilitiesCount)
    {
        SecurityTestingAgentId = securityTestingAgentId;
        TestId = testId;
        VulnerabilitiesCount = vulnerabilitiesCount;
        OccurredOn = DateTime.UtcNow;
    }
}
