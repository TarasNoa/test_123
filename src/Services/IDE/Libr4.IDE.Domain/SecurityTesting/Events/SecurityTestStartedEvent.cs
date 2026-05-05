using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SecurityTesting.Events;

/// <summary>
/// Domain event raised when security test is started
/// </summary>
public class SecurityTestStartedEvent : IDomainEvent
{
    public Guid SecurityTestingAgentId { get; }
    public string TestId { get; }
    public DateTime OccurredOn { get; }
    
    public SecurityTestStartedEvent(
        Guid securityTestingAgentId,
        string testId)
    {
        SecurityTestingAgentId = securityTestingAgentId;
        TestId = testId;
        OccurredOn = DateTime.UtcNow;
    }
}
